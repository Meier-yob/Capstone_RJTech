using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Services;

public sealed class ReportRefreshService(ApplicationDbContext db, ReportUpdateTracker reportUpdates)
{
    private static readonly SemaphoreSlim RefreshLock = new(1, 1);
    private static readonly string[] ReportStatuses = ["Paid", "Ongoing", "Refunded"];

    public async Task EnsureGeneratedAsync(CancellationToken cancellationToken = default)
    {
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            bool hasReports = await HasOverviewReportsAsync(cancellationToken);
            if (!hasReports || reportUpdates.NeedsRefresh)
                await RefreshUntilCurrentAsync(cancellationToken);
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await RefreshLock.WaitAsync(cancellationToken);
        try
        {
            await RefreshUntilCurrentAsync(cancellationToken);
        }
        finally
        {
            RefreshLock.Release();
        }
    }

    private async Task<bool> HasOverviewReportsAsync(CancellationToken cancellationToken)
        => await db.SalesOverviews.AsNoTracking().AnyAsync(cancellationToken)
            && await db.InventoryOverviews.AsNoTracking().AnyAsync(cancellationToken)
            && await db.DeliveryOverviews.AsNoTracking().AnyAsync(cancellationToken);

    private async Task RefreshUntilCurrentAsync(CancellationToken cancellationToken)
    {
        long sourceVersion;
        do
        {
            sourceVersion = reportUpdates.SourceVersion;
            await RefreshCoreAsync(cancellationToken);
            reportUpdates.MarkRefreshed(sourceVersion);
        }
        while (sourceVersion != reportUpdates.SourceVersion);
    }

    private async Task RefreshCoreAsync(CancellationToken cancellationToken)
    {
        var generatedAt = DateTime.Now;
        var today = generatedAt.Date;

        var checkouts = await db.Checkouts.AsNoTracking()
            .Where(checkout => ReportStatuses.Contains(checkout.Status))
            .Select(checkout => new CheckoutSource(
                checkout.CheckoutID,
                checkout.CustomerID,
                checkout.TotalAmount,
                checkout.PaymentMethod,
                checkout.PaymentType,
                checkout.DatePurchased,
                checkout.Status))
            .ToListAsync(cancellationToken);

        var paidCheckoutIds = checkouts.Where(checkout => checkout.Status == "Paid")
            .Select(checkout => checkout.CheckoutID).ToHashSet();
        var paidCheckouts = checkouts.Where(checkout => checkout.Status == "Paid").ToList();

        var checkoutItems = await db.CheckoutItems.AsNoTracking()
            .Where(item => paidCheckoutIds.Contains(item.CheckoutID))
            .Select(item => new ItemSource(item.CheckoutID, item.ProductID, item.ItemQuantity, item.SubTotal))
            .ToListAsync(cancellationToken);
        var itemTotals = checkoutItems.GroupBy(item => item.CheckoutID)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));
        var checkoutDates = paidCheckouts.ToDictionary(checkout => checkout.CheckoutID, checkout => checkout.DatePurchased);

        var products = await db.Products.AsNoTracking()
            .Select(product => new ProductSource(
                product.product_ID,
                product.category_ID,
                product.product_quantity,
                product.reorder_level,
                product.product_status))
            .ToListAsync(cancellationToken);
        var productCategories = products.ToDictionary(product => product.ProductID, product => product.CategoryID);
        var categories = await db.ProductCategories.AsNoTracking()
            .Select(category => category.category_ID)
            .ToListAsync(cancellationToken);

        var deliveries = await db.Deliveries.AsNoTracking()
            .Select(delivery => new DeliverySource(delivery.delivery_ID, delivery.date_delivered))
            .ToListAsync(cancellationToken);
        var deliveryDates = deliveries.ToDictionary(delivery => delivery.DeliveryID, delivery => delivery.DateDelivered);
        var deliveryDetails = await db.DeliveryDetails.AsNoTracking()
            .Select(detail => new DeliveryItemSource(detail.delivery_ID, detail.product_ID, detail.product_quantity))
            .ToListAsync(cancellationToken);

        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        await ClearReportTablesAsync(cancellationToken);

        var weekStart = SalesPeriod.StartOfWeek(today);
        var salesOverview = new SalesOverviewReport
        {
            TotalSales = paidCheckouts.Sum(checkout => checkout.TotalAmount),
            TotalTransactions = checkouts.Count,
            TotalItemsSold = checkoutItems.Sum(item => item.Quantity),
            TotalCustomers = await db.Customers.AsNoTracking().CountAsync(cancellationToken),
            TodaySales = paidCheckouts.Where(checkout => checkout.DatePurchased >= today && checkout.DatePurchased < today.AddDays(1)).Sum(checkout => checkout.TotalAmount),
            WeeklySales = paidCheckouts.Where(checkout => checkout.DatePurchased >= weekStart && checkout.DatePurchased < weekStart.AddDays(7)).Sum(checkout => checkout.TotalAmount),
            MonthlySales = paidCheckouts.Where(checkout => checkout.DatePurchased.Year == today.Year && checkout.DatePurchased.Month == today.Month).Sum(checkout => checkout.TotalAmount),
            YearlySales = paidCheckouts.Where(checkout => checkout.DatePurchased.Year == today.Year).Sum(checkout => checkout.TotalAmount),
            LastSaleDate = paidCheckouts.Count == 0 ? null : paidCheckouts.Max(checkout => checkout.DatePurchased),
            LastUpdated = generatedAt
        };
        db.SalesOverviews.Add(salesOverview);

        db.WeeklySales.AddRange(paidCheckouts
            .GroupBy(checkout => SalesPeriod.StartOfWeek(checkout.DatePurchased.Date))
            .OrderBy(group => group.Key)
            .Select(group => new WeeklySalesReport
            {
                WeekStartDate = group.Key,
                WeekEndDate = group.Key.AddDays(6),
                TotalSales = group.Sum(checkout => checkout.TotalAmount),
                TotalTransactions = group.Count(),
                TotalItemsSold = group.Sum(checkout => itemTotals.GetValueOrDefault(checkout.CheckoutID))
            }));

        db.MonthlySales.AddRange(paidCheckouts
            .GroupBy(checkout => new { checkout.DatePurchased.Year, checkout.DatePurchased.Month })
            .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.Month)
            .Select(group => new MonthlySalesReport
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                TotalSales = group.Sum(checkout => checkout.TotalAmount),
                TotalTransactions = group.Count(),
                TotalItemsSold = group.Sum(checkout => itemTotals.GetValueOrDefault(checkout.CheckoutID))
            }));

        db.YearlySales.AddRange(paidCheckouts
            .GroupBy(checkout => checkout.DatePurchased.Year)
            .OrderBy(group => group.Key)
            .Select(group => new YearlySalesReport
            {
                Year = group.Key,
                TotalSales = group.Sum(checkout => checkout.TotalAmount),
                TotalTransactions = group.Count(),
                TotalItemsSold = group.Sum(checkout => itemTotals.GetValueOrDefault(checkout.CheckoutID))
            }));

        var monthlyProductSales = checkoutItems
            .Select(item => new
            {
                item.ProductID,
                item.Quantity,
                item.Amount,
                Period = checkoutDates[item.CheckoutID].ToString("yyyy-MM")
            })
            .GroupBy(item => new { item.Period, item.ProductID })
            .Select(group => new ProductSalesSource(
                group.Key.Period,
                group.Key.ProductID,
                group.Sum(item => item.Quantity),
                group.Sum(item => item.Amount)))
            .ToList();

        foreach (var period in monthlyProductSales.GroupBy(item => item.Period))
        {
            var best = period.OrderByDescending(item => item.Quantity)
                .ThenByDescending(item => item.Amount).ThenBy(item => item.ProductID).First();
            var least = period.OrderBy(item => item.Quantity)
                .ThenBy(item => item.Amount).ThenBy(item => item.ProductID).First();
            db.BestSellingProducts.Add(new BestSellingProductReport
            {
                ProductID = best.ProductID,
                TotalQuantitySold = best.Quantity,
                TotalSalesAmount = best.Amount,
                Period = period.Key,
                DateGenerated = generatedAt
            });
            db.LeastSellingProducts.Add(new LeastSellingProductReport
            {
                ProductID = least.ProductID,
                TotalQuantitySold = least.Quantity,
                TotalSalesAmount = least.Amount,
                Period = period.Key,
                DateGenerated = generatedAt
            });
        }

        db.SalesByCategories.AddRange(monthlyProductSales
            .Where(item => productCategories.ContainsKey(item.ProductID))
            .GroupBy(item => new { item.Period, CategoryID = productCategories[item.ProductID] })
            .Select(group => new SalesByCategoryReport
            {
                Period = group.Key.Period,
                CategoryID = group.Key.CategoryID,
                TotalQuantitySold = group.Sum(item => item.Quantity),
                TotalSalesAmount = group.Sum(item => item.Amount),
                DateGenerated = generatedAt
            }));

        db.ReportTransactions.AddRange(checkouts.OrderBy(checkout => checkout.DatePurchased)
            .ThenBy(checkout => checkout.CheckoutID)
            .Select(checkout => new TransactionReport
            {
                CheckoutID = checkout.CheckoutID,
                CustomerID = checkout.CustomerID,
                PaymentType = checkout.PaymentType,
                PaymentMethod = checkout.PaymentMethod,
                Amount = checkout.TotalAmount,
                TransactionDate = checkout.DatePurchased,
                Status = checkout.Status
            }));

        var stockRows = products.Select(product => new ProductStockSummaryReport
        {
            ProductID = product.ProductID,
            CurrentQuantity = product.Quantity,
            ReorderLevel = product.ReorderLevel,
            StockStatus = StockStatus(product),
            LastUpdated = generatedAt
        }).ToList();
        db.ProductStockSummaries.AddRange(stockRows);
        db.InventoryOverviews.Add(new InventoryOverviewReport
        {
            TotalProducts = products.Count,
            TotalQuantity = products.Sum(product => product.Quantity),
            AvailableProducts = stockRows.Count(row => row.StockStatus == "Available"),
            UnavailableProducts = stockRows.Count(row => row.StockStatus == "Unavailable"),
            LowStockProducts = stockRows.Count(row => row.StockStatus == "Low Stock"),
            OutOfStockProducts = stockRows.Count(row => row.StockStatus == "Out of Stock"),
            LastUpdated = generatedAt
        });

        db.ProductCategoryOverviews.AddRange(categories.Select(categoryId =>
        {
            var categoryProducts = products.Where(product => product.CategoryID == categoryId).ToList();
            return new ProductCategoryOverviewReport
            {
                CategoryID = categoryId,
                TotalProducts = categoryProducts.Count,
                TotalQuantity = categoryProducts.Sum(product => product.Quantity),
                AvailableQuantity = categoryProducts.Where(product => StockStatus(product) is "Available" or "Low Stock").Sum(product => product.Quantity),
                UnavailableQuantity = categoryProducts.Where(product => StockStatus(product) is "Unavailable" or "Out of Stock").Sum(product => product.Quantity),
                LastUpdated = generatedAt
            };
        }));

        if (products.Count > 0)
        {
            var mostStocked = products.OrderByDescending(product => product.Quantity).ThenBy(product => product.ProductID).First();
            var leastStocked = products.OrderBy(product => product.Quantity).ThenBy(product => product.ProductID).First();
            db.MostStockedProducts.Add(new MostStockedProductReport
            {
                ProductID = mostStocked.ProductID,
                CurrentQuantity = mostStocked.Quantity,
                LastUpdated = generatedAt
            });
            db.LeastStockedProducts.Add(new LeastStockedProductReport
            {
                ProductID = leastStocked.ProductID,
                CurrentQuantity = leastStocked.Quantity,
                LastUpdated = generatedAt
            });
        }

        db.DeliveryOverviews.Add(new DeliveryOverviewReport
        {
            TotalDeliveries = deliveries.Count,
            TotalItemsDelivered = deliveryDetails.Sum(detail => detail.Quantity),
            TotalProductsDelivered = deliveryDetails.Select(detail => detail.ProductID).Distinct().Count(),
            LastDeliveryDate = deliveries.Count == 0 ? null : deliveries.Max(delivery => delivery.DateDelivered),
            LastUpdated = generatedAt
        });

        db.DeliverySummaries.AddRange(deliveries.Select(delivery =>
        {
            var details = deliveryDetails.Where(detail => detail.DeliveryID == delivery.DeliveryID).ToList();
            return new DeliverySummaryReport
            {
                DeliveryID = delivery.DeliveryID,
                TotalItems = details.Sum(detail => detail.Quantity),
                TotalProducts = details.Select(detail => detail.ProductID).Distinct().Count(),
                LastUpdated = generatedAt
            };
        }));

        db.ProductDeliverySummaries.AddRange(deliveryDetails
            .Where(detail => deliveryDates.ContainsKey(detail.DeliveryID))
            .GroupBy(detail => detail.ProductID)
            .Select(group => new ProductDeliverySummaryReport
            {
                ProductID = group.Key,
                TotalQuantityDelivered = group.Sum(detail => detail.Quantity),
                TotalDeliveries = group.Select(detail => detail.DeliveryID).Distinct().Count(),
                LastDeliveryDate = group.Max(detail => deliveryDates[detail.DeliveryID]),
                LastUpdated = generatedAt
            }));

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private async Task ClearReportTablesAsync(CancellationToken cancellationToken)
    {
        await db.BestSellingProducts.ExecuteDeleteAsync(cancellationToken);
        await db.LeastSellingProducts.ExecuteDeleteAsync(cancellationToken);
        await db.SalesByCategories.ExecuteDeleteAsync(cancellationToken);
        await db.ReportTransactions.ExecuteDeleteAsync(cancellationToken);
        await db.WeeklySales.ExecuteDeleteAsync(cancellationToken);
        await db.MonthlySales.ExecuteDeleteAsync(cancellationToken);
        await db.YearlySales.ExecuteDeleteAsync(cancellationToken);
        await db.SalesOverviews.ExecuteDeleteAsync(cancellationToken);
        await db.ProductStockSummaries.ExecuteDeleteAsync(cancellationToken);
        await db.ProductCategoryOverviews.ExecuteDeleteAsync(cancellationToken);
        await db.MostStockedProducts.ExecuteDeleteAsync(cancellationToken);
        await db.LeastStockedProducts.ExecuteDeleteAsync(cancellationToken);
        await db.InventoryOverviews.ExecuteDeleteAsync(cancellationToken);
        await db.DeliverySummaries.ExecuteDeleteAsync(cancellationToken);
        await db.ProductDeliverySummaries.ExecuteDeleteAsync(cancellationToken);
        await db.DeliveryOverviews.ExecuteDeleteAsync(cancellationToken);
    }

    private static string StockStatus(ProductSource product)
    {
        if (product.Status == "Unavailable") return "Unavailable";
        if (product.Quantity <= 0) return "Out of Stock";
        if (product.Quantity <= product.ReorderLevel) return "Low Stock";
        return "Available";
    }

    private sealed record CheckoutSource(int CheckoutID, int CustomerID, decimal TotalAmount, string PaymentMethod,
        string PaymentType, DateTime DatePurchased, string Status);
    private sealed record ItemSource(int CheckoutID, int ProductID, int Quantity, decimal Amount);
    private sealed record ProductSource(int ProductID, int CategoryID, int Quantity, int ReorderLevel, string Status);
    private sealed record ProductSalesSource(string Period, int ProductID, int Quantity, decimal Amount);
    private sealed record DeliverySource(int DeliveryID, DateTime DateDelivered);
    private sealed record DeliveryItemSource(int DeliveryID, int ProductID, int Quantity);
}
