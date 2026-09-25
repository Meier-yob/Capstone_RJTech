using Capstone_RJTech.Data;
using Capstone_RJTech.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Services;

/// <summary>
/// Computes every report data set directly from the live inventory and sales
/// tables on demand. Replaces the old snapshot-table pipeline: nothing is
/// persisted, so the Reports page is always current.
/// </summary>
public sealed class ReportComputationService(ApplicationDbContext db)
{
    private static readonly string[] ReportStatuses = ["Paid", "Ongoing", "Refunded"];

    public async Task<ReportSnapshot> BuildAsync(CancellationToken cancellationToken = default)
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
            LastSaleDate = paidCheckouts.Count == 0 ? null : paidCheckouts.Max(checkout => checkout.DatePurchased)
        };

        var weeklySales = paidCheckouts
            .GroupBy(checkout => SalesPeriod.StartOfWeek(checkout.DatePurchased.Date))
            .OrderBy(group => group.Key)
            .Select(group => new WeeklySalesReport
            {
                WeekStartDate = group.Key,
                WeekEndDate = group.Key.AddDays(6),
                TotalSales = group.Sum(checkout => checkout.TotalAmount),
                TotalTransactions = group.Count(),
                TotalItemsSold = group.Sum(checkout => itemTotals.GetValueOrDefault(checkout.CheckoutID))
            }).ToList();

        var monthlySales = paidCheckouts
            .GroupBy(checkout => new { checkout.DatePurchased.Year, checkout.DatePurchased.Month })
            .OrderBy(group => group.Key.Year).ThenBy(group => group.Key.Month)
            .Select(group => new MonthlySalesReport
            {
                Year = group.Key.Year,
                Month = group.Key.Month,
                TotalSales = group.Sum(checkout => checkout.TotalAmount),
                TotalTransactions = group.Count(),
                TotalItemsSold = group.Sum(checkout => itemTotals.GetValueOrDefault(checkout.CheckoutID))
            }).ToList();

        var yearlySales = paidCheckouts
            .GroupBy(checkout => checkout.DatePurchased.Year)
            .OrderBy(group => group.Key)
            .Select(group => new YearlySalesReport
            {
                Year = group.Key,
                TotalSales = group.Sum(checkout => checkout.TotalAmount),
                TotalTransactions = group.Count(),
                TotalItemsSold = group.Sum(checkout => itemTotals.GetValueOrDefault(checkout.CheckoutID))
            }).ToList();

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

        var bestSelling = new List<BestSellingProductReport>();
        var leastSelling = new List<LeastSellingProductReport>();
        foreach (var period in monthlyProductSales.GroupBy(item => item.Period))
        {
            var best = period.OrderByDescending(item => item.Quantity)
                .ThenByDescending(item => item.Amount).ThenBy(item => item.ProductID).First();
            var least = period.OrderBy(item => item.Quantity)
                .ThenBy(item => item.Amount).ThenBy(item => item.ProductID).First();
            bestSelling.Add(new BestSellingProductReport
            {
                ProductID = best.ProductID,
                TotalQuantitySold = best.Quantity,
                TotalSalesAmount = best.Amount,
                Period = period.Key,
                DateGenerated = generatedAt
            });
            leastSelling.Add(new LeastSellingProductReport
            {
                ProductID = least.ProductID,
                TotalQuantitySold = least.Quantity,
                TotalSalesAmount = least.Amount,
                Period = period.Key,
                DateGenerated = generatedAt
            });
        }

        var salesByCategories = monthlyProductSales
            .Where(item => productCategories.ContainsKey(item.ProductID))
            .GroupBy(item => new { item.Period, CategoryID = productCategories[item.ProductID] })
            .Select(group => new SalesByCategoryReport
            {
                Period = group.Key.Period,
                CategoryID = group.Key.CategoryID,
                TotalQuantitySold = group.Sum(item => item.Quantity),
                TotalSalesAmount = group.Sum(item => item.Amount),
                DateGenerated = generatedAt
            }).ToList();

        var transactions = checkouts.OrderBy(checkout => checkout.DatePurchased)
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
            }).ToList();

        var stockRows = products.Select(product => new ProductStockSummaryReport
        {
            ProductID = product.ProductID,
            CurrentQuantity = product.Quantity,
            ReorderLevel = product.ReorderLevel,
            StockStatus = StockStatus(product)
        }).ToList();

        var inventoryOverview = new InventoryOverviewReport
        {
            TotalProducts = products.Count,
            TotalQuantity = products.Sum(product => product.Quantity),
            AvailableProducts = stockRows.Count(row => row.StockStatus == "Available"),
            UnavailableProducts = stockRows.Count(row => row.StockStatus == "Unavailable"),
            LowStockProducts = stockRows.Count(row => row.StockStatus == "Low Stock"),
            OutOfStockProducts = stockRows.Count(row => row.StockStatus == "Out of Stock")
        };

        var categoryOverviews = categories.Select(categoryId =>
        {
            var categoryProducts = products.Where(product => product.CategoryID == categoryId).ToList();
            return new ProductCategoryOverviewReport
            {
                CategoryID = categoryId,
                TotalProducts = categoryProducts.Count,
                TotalQuantity = categoryProducts.Sum(product => product.Quantity),
                AvailableQuantity = categoryProducts.Where(product => StockStatus(product) is "Available" or "Low Stock").Sum(product => product.Quantity),
                UnavailableQuantity = categoryProducts.Where(product => StockStatus(product) is "Unavailable" or "Out of Stock").Sum(product => product.Quantity),
                LowStockProducts = categoryProducts.Count(product => StockStatus(product) == "Low Stock"),
                OutOfStockProducts = categoryProducts.Count(product => StockStatus(product) == "Out of Stock")
            };
        }).ToList();

        var mostStocked = new List<MostStockedProductReport>();
        var leastStocked = new List<LeastStockedProductReport>();
        if (products.Count > 0)
        {
            var most = products.OrderByDescending(product => product.Quantity).ThenBy(product => product.ProductID).First();
            var least = products.OrderBy(product => product.Quantity).ThenBy(product => product.ProductID).First();
            mostStocked.Add(new MostStockedProductReport
            {
                ProductID = most.ProductID,
                CurrentQuantity = most.Quantity
            });
            leastStocked.Add(new LeastStockedProductReport
            {
                ProductID = least.ProductID,
                CurrentQuantity = least.Quantity
            });
        }

        var deliveryOverview = new DeliveryOverviewReport
        {
            TotalDeliveries = deliveries.Count,
            TotalItemsDelivered = deliveryDetails.Sum(detail => detail.Quantity),
            TotalProductsDelivered = deliveryDetails.Select(detail => detail.ProductID).Distinct().Count(),
            LastDeliveryDate = deliveries.Count == 0 ? null : deliveries.Max(delivery => delivery.DateDelivered)
        };

        var deliverySummaries = deliveries.Select(delivery =>
        {
            var details = deliveryDetails.Where(detail => detail.DeliveryID == delivery.DeliveryID).ToList();
            return new DeliverySummaryReport
            {
                DeliveryID = delivery.DeliveryID,
                TotalItems = details.Sum(detail => detail.Quantity),
                TotalProducts = details.Select(detail => detail.ProductID).Distinct().Count()
            };
        }).ToList();

        var productDeliverySummaries = deliveryDetails
            .Where(detail => deliveryDates.ContainsKey(detail.DeliveryID))
            .GroupBy(detail => detail.ProductID)
            .Select(group => new ProductDeliverySummaryReport
            {
                ProductID = group.Key,
                TotalQuantityDelivered = group.Sum(detail => detail.Quantity),
                TotalDeliveries = group.Select(detail => detail.DeliveryID).Distinct().Count(),
                LastDeliveryDate = group.Max(detail => deliveryDates[detail.DeliveryID])
            }).ToList();

        return new ReportSnapshot(
            salesOverview,
            weeklySales,
            monthlySales,
            yearlySales,
            bestSelling,
            leastSelling,
            salesByCategories,
            transactions,
            inventoryOverview,
            stockRows,
            categoryOverviews,
            mostStocked,
            leastStocked,
            deliveryOverview,
            deliverySummaries,
            productDeliverySummaries);
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

/// <summary>Every report data set, computed together in a single pass.</summary>
public sealed record ReportSnapshot(
    SalesOverviewReport SalesOverview,
    List<WeeklySalesReport> WeeklySales,
    List<MonthlySalesReport> MonthlySales,
    List<YearlySalesReport> YearlySales,
    List<BestSellingProductReport> BestSellingProducts,
    List<LeastSellingProductReport> LeastSellingProducts,
    List<SalesByCategoryReport> SalesByCategories,
    List<TransactionReport> Transactions,
    InventoryOverviewReport InventoryOverview,
    List<ProductStockSummaryReport> ProductStock,
    List<ProductCategoryOverviewReport> CategoryOverviews,
    List<MostStockedProductReport> MostStockedProducts,
    List<LeastStockedProductReport> LeastStockedProducts,
    DeliveryOverviewReport DeliveryOverview,
    List<DeliverySummaryReport> DeliverySummaries,
    List<ProductDeliverySummaryReport> ProductDeliverySummaries);