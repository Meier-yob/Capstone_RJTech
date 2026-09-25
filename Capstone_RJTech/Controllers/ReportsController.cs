using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace Capstone_RJTech.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class ReportsController(
    ApplicationDbContext db,
    ReportComputationService reportComputation,
    IExcelExportService excelExport) : Controller
{
    private static readonly string[] ReportStatuses = ["Paid", "Ongoing", "Refunded"];
    private static readonly string[] StockStatuses = ["Available", "Unavailable", "Low Stock", "Out of Stock"];

    [HttpGet("/Reports")]
    [HttpGet("/Reports/Index")]
    public async Task<IActionResult> Index(
        string? tab,
        string? period,
        int? month,
        int? year,
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken)
    {
        var model = await BuildModelAsync(tab, period, month, year, search, status, sort, cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> Export(
        string? tab,
        string? period,
        int? month,
        int? year,
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken)
    {
        var model = await BuildModelAsync(tab, period, month, year, search, status, sort, cancellationToken);
        try
        {
            return model.ActiveTab switch
            {
                "inventory" => ExportInventory(model.Inventory),
                "delivery" => ExportDelivery(model.Delivery),
                _ => ExportSales(model.Sales)
            };
        }
        catch (InvalidOperationException exception)
        {
            TempData["ToastWarning"] = exception.Message;
            return RedirectToAction(nameof(Index), new { tab, period, month, year, search, status, sort });
        }
    }

    private async Task<ReportsViewModel> BuildModelAsync(
        string? tab,
        string? period,
        int? month,
        int? year,
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken)
    {
        var snapshot = await reportComputation.BuildAsync(cancellationToken);
        var lookups = await BuildLookupsAsync(cancellationToken);
        var today = DateTime.Today;
        int selectedYear = year is >= 1900 and <= 9998 ? year.Value : today.Year;
        int selectedMonth = month is >= 1 and <= 12 ? month.Value : today.Month;
        var model = new ReportsViewModel
        {
            ActiveTab = NormalizeTab(tab),
            Period = NormalizePeriod(period),
            Month = selectedMonth,
            Year = selectedYear,
            Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
            Status = string.IsNullOrWhiteSpace(status) ? null : status.Trim(),
            Sort = string.IsNullOrWhiteSpace(sort) ? null : sort.Trim()
        };

        model.Years = snapshot.MonthlySales.Select(report => report.Year)
            .Concat(snapshot.YearlySales.Select(report => report.Year))
            .Distinct().OrderByDescending(value => value).ToList();
        model.Years = model.Years.Append(today.Year).Append(selectedYear).Distinct().OrderDescending().ToList();

        switch (model.ActiveTab)
        {
            case "inventory":
                model.Inventory = BuildInventory(model, snapshot, lookups);
                break;
            case "delivery":
                model.Delivery = BuildDelivery(model, snapshot, lookups);
                break;
            default:
                model.Sales = BuildSales(model, snapshot, lookups);
                break;
        }

        return model;
    }

    private async Task<ReportLookups> BuildLookupsAsync(CancellationToken cancellationToken)
    {
        var products = await db.Products.AsNoTracking().Include(product => product!.Category)
            .ToListAsync(cancellationToken);
        var productInfo = products.ToDictionary(
            product => product.product_ID,
            product => (Name: product.product_name, CategoryName: product.Category?.category_name));
        var categoryNames = await db.ProductCategories.AsNoTracking()
            .ToDictionaryAsync(category => category.category_ID, category => category.category_name, cancellationToken);
        var customerNames = await db.Customers.AsNoTracking()
            .ToDictionaryAsync(customer => customer.customer_ID, customer => customer.customer_FullName, cancellationToken);
        var checkoutPaymentTypes = await db.Checkouts.AsNoTracking()
            .ToDictionaryAsync(checkout => checkout.CheckoutID, checkout => checkout.PaymentType, cancellationToken);
        return new ReportLookups(productInfo, categoryNames, customerNames, checkoutPaymentTypes);
    }

    private static SalesOverviewViewModel BuildSales(
        ReportsViewModel filters, ReportSnapshot snapshot, ReportLookups lookups)
    {
        var source = snapshot.SalesOverview;
        var model = new SalesOverviewViewModel
        {
            TotalSales = source.TotalSales,
            TotalCheckouts = source.TotalTransactions,
            TotalSoldItems = source.TotalItemsSold,
            TotalCustomers = source.TotalCustomers,
            TodaySales = source.TodaySales,
            WeeklySales = source.WeeklySales,
            MonthlySales = source.MonthlySales,
            YearlySales = source.YearlySales,
            LastSaleDate = source.LastSaleDate
        };

        if (filters.Period == "monthly")
        {
            model.History = snapshot.MonthlySales
                .Where(report => report.Year == filters.Year)
                .OrderBy(report => report.Month)
                .Select(report => new ReportChartPoint
                {
                    Label = CultureInfo.CurrentCulture.DateTimeFormat.GetAbbreviatedMonthName(report.Month),
                    Value = report.TotalSales,
                    TotalTransactions = report.TotalTransactions,
                    TotalItems = report.TotalItemsSold
                }).ToList();
        }
        else if (filters.Period == "yearly")
        {
            model.History = snapshot.YearlySales.OrderBy(report => report.Year)
                .Select(report => new ReportChartPoint
                {
                    Label = report.Year.ToString(CultureInfo.InvariantCulture),
                    Value = report.TotalSales,
                    TotalTransactions = report.TotalTransactions,
                    TotalItems = report.TotalItemsSold
                }).ToList();
        }
        else
        {
            var start = new DateTime(filters.Year, filters.Month, 1);
            var end = start.AddMonths(1);
            model.History = snapshot.WeeklySales
                .Where(report => report.WeekStartDate < end && report.WeekEndDate >= start)
                .OrderBy(report => report.WeekStartDate)
                .Select(report => new ReportChartPoint
                {
                    Label = $"{report.WeekStartDate:MMM d}–{report.WeekEndDate:MMM d}",
                    Value = report.TotalSales,
                    TotalTransactions = report.TotalTransactions,
                    TotalItems = report.TotalItemsSold
                }).ToList();
        }

        string periodKey = $"{filters.Year:D4}-{filters.Month:D2}";
        model.BestSellingProduct = ToProductSalesRow(
            snapshot.BestSellingProducts.FirstOrDefault(report => report.Period == periodKey), lookups);
        model.LeastSellingProduct = ToProductSalesRow(
            snapshot.LeastSellingProducts.FirstOrDefault(report => report.Period == periodKey), lookups);

        model.SalesByCategory = snapshot.SalesByCategories
            .Where(report => report.Period == periodKey)
            .OrderByDescending(report => report.TotalSalesAmount)
            .Select(report => new CategorySalesReportRow
            {
                CategoryID = report.CategoryID,
                CategoryName = lookups.CategoryName(report.CategoryID),
                TotalQuantitySold = report.TotalQuantitySold,
                TotalSalesAmount = report.TotalSalesAmount
            }).ToList();

        var transactions = snapshot.Transactions.AsEnumerable();
        if (filters.Status is not null && ReportStatuses.Contains(filters.Status, StringComparer.OrdinalIgnoreCase))
            transactions = transactions.Where(report => report.Status.Equals(filters.Status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string search = filters.Search;
            int? checkoutId = ParseFormattedId(search, "CHK-");
            transactions = transactions.Where(report =>
                lookups.CustomerName(report.CustomerID).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                report.PaymentMethod.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                report.Status.Contains(search, StringComparison.OrdinalIgnoreCase) ||
                lookups.CheckoutPaymentType(report.CheckoutID).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (checkoutId.HasValue && report.CheckoutID == checkoutId.Value));
        }
        transactions = filters.Sort switch
        {
            "amount-asc" => transactions.OrderBy(report => report.Amount),
            "amount-desc" => transactions.OrderByDescending(report => report.Amount),
            "date-asc" => transactions.OrderBy(report => report.TransactionDate),
            _ => transactions.OrderByDescending(report => report.TransactionDate).ThenByDescending(report => report.TransactionID)
        };
        model.RecentTransactions = transactions.Take(12).Select(report => new TransactionReportRow
        {
            CheckoutID = report.CheckoutID,
            CustomerName = lookups.CustomerName(report.CustomerID),
            PaymentType = lookups.CheckoutPaymentType(report.CheckoutID),
            PaymentMethod = report.PaymentMethod,
            Amount = report.Amount,
            TransactionDate = report.TransactionDate,
            Status = report.Status
        }).ToList();
        return model;
    }

    private static InventoryOverviewViewModel BuildInventory(
        ReportsViewModel filters, ReportSnapshot snapshot, ReportLookups lookups)
    {
        var source = snapshot.InventoryOverview;
        var model = new InventoryOverviewViewModel
        {
            TotalProducts = source.TotalProducts,
            TotalQuantity = source.TotalQuantity,
            AvailableProducts = source.AvailableProducts,
            UnavailableProducts = source.UnavailableProducts,
            LowStockProducts = source.LowStockProducts,
            OutOfStockProducts = source.OutOfStockProducts
        };

        var stock = snapshot.ProductStock.AsEnumerable();
        if (filters.Status is not null && StockStatuses.Contains(filters.Status, StringComparer.OrdinalIgnoreCase))
            stock = stock.Where(report => report.StockStatus.Equals(filters.Status, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string search = filters.Search;
            int? productId = ParseFormattedId(search);
            stock = stock.Where(report =>
                lookups.ProductName(report.ProductID).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                lookups.ProductCategoryName(report.ProductID).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (productId.HasValue && report.ProductID == productId.Value));
        }
        stock = filters.Sort switch
        {
            "quantity-asc" => stock.OrderBy(report => report.CurrentQuantity),
            "quantity-desc" => stock.OrderByDescending(report => report.CurrentQuantity),
            "name" => stock.OrderBy(report => lookups.ProductName(report.ProductID), StringComparer.OrdinalIgnoreCase),
            _ => stock.OrderBy(report => report.ProductID)
        };
        var stockSources = stock.ToList();
        model.ProductStock = stockSources.Select(report => new ProductStockReportRow
        {
            ProductID = report.ProductID,
            ProductCode = ProductController.FormatProductCode(lookups.ProductCategoryName(report.ProductID), report.ProductID),
            ProductName = lookups.ProductName(report.ProductID),
            CategoryName = lookups.ProductCategoryName(report.ProductID),
            CurrentQuantity = report.CurrentQuantity,
            ReorderLevel = report.ReorderLevel,
            StockStatus = report.StockStatus
        }).ToList();

        model.ProductCategories = snapshot.CategoryOverviews
            .OrderByDescending(report => report.TotalQuantity)
            .Select(report => new CategoryInventoryReportRow
            {
                CategoryName = lookups.CategoryName(report.CategoryID),
                TotalProducts = report.TotalProducts,
                TotalQuantity = report.TotalQuantity,
                AvailableQuantity = report.AvailableQuantity,
                UnavailableQuantity = report.UnavailableQuantity,
                LowStockProducts = report.LowStockProducts,
                OutOfStockProducts = report.OutOfStockProducts
            }).ToList();

        model.MostStockedProduct = ToStockExtremeRow(snapshot.MostStockedProducts.FirstOrDefault(), lookups);
        model.LeastStockedProduct = ToStockExtremeRow(snapshot.LeastStockedProducts.FirstOrDefault(), lookups);
        return model;
    }

    private static DeliveryOverviewViewModel BuildDelivery(
        ReportsViewModel filters, ReportSnapshot snapshot, ReportLookups lookups)
    {
        var source = snapshot.DeliveryOverview;
        var model = new DeliveryOverviewViewModel
        {
            TotalDeliveries = source.TotalDeliveries,
            TotalItemsDelivered = source.TotalItemsDelivered,
            TotalProductsDelivered = source.TotalProductsDelivered,
            LastDeliveryDate = source.LastDeliveryDate
        };

        int? deliveryId = ParseFormattedId(filters.Search, "DEL-");
        var deliveries = snapshot.DeliverySummaries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filters.Search) && deliveryId.HasValue)
            deliveries = deliveries.Where(report => report.DeliveryID == deliveryId.Value);
        deliveries = filters.Sort switch
        {
            "items-asc" => deliveries.OrderBy(report => report.TotalItems),
            "items-desc" => deliveries.OrderByDescending(report => report.TotalItems),
            _ => deliveries.OrderByDescending(report => report.DeliveryID)
        };
        model.DeliverySummaries = deliveries.Take(20).Select(report => new DeliverySummaryReportRow
        {
            DeliveryID = report.DeliveryID,
            TotalItems = report.TotalItems,
            TotalProducts = report.TotalProducts
        }).ToList();

        var productDeliveries = snapshot.ProductDeliverySummaries.AsEnumerable();
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string search = filters.Search;
            int? productId = ParseFormattedId(search);
            productDeliveries = productDeliveries.Where(report =>
                lookups.ProductName(report.ProductID).Contains(search, StringComparison.OrdinalIgnoreCase) ||
                (productId.HasValue && report.ProductID == productId.Value));
        }
        productDeliveries = filters.Sort switch
        {
            "quantity-asc" => productDeliveries.OrderBy(report => report.TotalQuantityDelivered),
            "quantity-desc" => productDeliveries.OrderByDescending(report => report.TotalQuantityDelivered),
            "deliveries-desc" => productDeliveries.OrderByDescending(report => report.TotalDeliveries),
            "date-asc" => productDeliveries.OrderBy(report => report.LastDeliveryDate),
            _ => productDeliveries.OrderByDescending(report => report.LastDeliveryDate)
        };
        var productSources = productDeliveries.Take(20).ToList();
        model.ProductDeliveries = productSources.Select(report => new ProductDeliveryReportRow
        {
            ProductID = report.ProductID,
            ProductCode = ProductController.FormatProductCode(lookups.ProductCategoryName(report.ProductID), report.ProductID),
            ProductName = lookups.ProductName(report.ProductID),
            TotalQuantityDelivered = report.TotalQuantityDelivered,
            TotalDeliveries = report.TotalDeliveries,
            LastDeliveryDate = report.LastDeliveryDate
        }).ToList();
        return model;
    }

    private FileContentResult ExportSales(SalesOverviewViewModel model)
    {
        var columns = new[]
        {
            new ExcelExportColumn("Checkout ID"), new ExcelExportColumn("Customer Name"),
            new ExcelExportColumn("Payment Type"), new ExcelExportColumn("Payment Method"),
            new ExcelExportColumn("Amount", ExcelColumnType.Currency),
            new ExcelExportColumn("Transaction Date", ExcelColumnType.Date),
            new ExcelExportColumn("Status", ExcelColumnType.Status)
        };
        var rows = model.RecentTransactions.Select(row => (IReadOnlyList<object?>)
            [row.CheckoutCode, row.CustomerName, row.PaymentType, row.PaymentMethod, row.Amount, row.TransactionDate, row.Status]);
        return ExcelFile("Sales Overview", "ReportsSales", columns, rows);
    }

    private FileContentResult ExportInventory(InventoryOverviewViewModel model)
    {
        var columns = new[]
        {
            new ExcelExportColumn("Product ID"), new ExcelExportColumn("Product Name"),
            new ExcelExportColumn("Category Name"), new ExcelExportColumn("Current Quantity", ExcelColumnType.Integer),
            new ExcelExportColumn("Reorder Level", ExcelColumnType.Integer),
            new ExcelExportColumn("Stock Status", ExcelColumnType.Status)
        };
        var rows = model.ProductStock.Select(row => (IReadOnlyList<object?>)
            [row.ProductCode, row.ProductName, row.CategoryName, row.CurrentQuantity, row.ReorderLevel, row.StockStatus]);
        return ExcelFile("Inventory Overview", "ReportsInventory", columns, rows);
    }

    private FileContentResult ExportDelivery(DeliveryOverviewViewModel model)
    {
        var columns = new[]
        {
            new ExcelExportColumn("Product ID"), new ExcelExportColumn("Product Name"),
            new ExcelExportColumn("Total Quantity Delivered", ExcelColumnType.Integer),
            new ExcelExportColumn("Total Deliveries", ExcelColumnType.Integer),
            new ExcelExportColumn("Last Delivery Date", ExcelColumnType.Date)
        };
        var rows = model.ProductDeliveries.Select(row => (IReadOnlyList<object?>)
            [row.ProductCode, row.ProductName, row.TotalQuantityDelivered, row.TotalDeliveries, row.LastDeliveryDate]);
        return ExcelFile("Delivery Overview", "ReportsDelivery", columns, rows);
    }

    private FileContentResult ExcelFile(
        string title,
        string fileName,
        IReadOnlyList<ExcelExportColumn> columns,
        IEnumerable<IReadOnlyList<object?>> rows)
        => File(excelExport.ExportToExcel(title, columns, rows), ExcelExportFile.ContentType,
            ExcelExportFile.CreateFileName(fileName));

    private static ProductSalesReportRow? ToProductSalesRow(BestSellingProductReport? source, ReportLookups lookups)
        => source == null ? null : new ProductSalesReportRow
        {
            ProductID = source.ProductID,
            ProductCode = ProductController.FormatProductCode(lookups.ProductCategoryName(source.ProductID), source.ProductID),
            ProductName = lookups.ProductName(source.ProductID),
            TotalQuantitySold = source.TotalQuantitySold,
            TotalSalesAmount = source.TotalSalesAmount
        };

    private static ProductSalesReportRow? ToProductSalesRow(LeastSellingProductReport? source, ReportLookups lookups)
        => source == null ? null : new ProductSalesReportRow
        {
            ProductID = source.ProductID,
            ProductCode = ProductController.FormatProductCode(lookups.ProductCategoryName(source.ProductID), source.ProductID),
            ProductName = lookups.ProductName(source.ProductID),
            TotalQuantitySold = source.TotalQuantitySold,
            TotalSalesAmount = source.TotalSalesAmount
        };

    private static StockExtremeReportRow? ToStockExtremeRow(MostStockedProductReport? source, ReportLookups lookups)
        => source == null ? null : new StockExtremeReportRow
        {
            ProductID = source.ProductID,
            ProductCode = ProductController.FormatProductCode(lookups.ProductCategoryName(source.ProductID), source.ProductID),
            ProductName = lookups.ProductName(source.ProductID),
            CurrentQuantity = source.CurrentQuantity
        };

    private static StockExtremeReportRow? ToStockExtremeRow(LeastStockedProductReport? source, ReportLookups lookups)
        => source == null ? null : new StockExtremeReportRow
        {
            ProductID = source.ProductID,
            ProductCode = ProductController.FormatProductCode(lookups.ProductCategoryName(source.ProductID), source.ProductID),
            ProductName = lookups.ProductName(source.ProductID),
            CurrentQuantity = source.CurrentQuantity
        };

    private static int? ParseFormattedId(string? value, string? prefix = null)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        string normalized = value.Trim();
        if (!string.IsNullOrEmpty(prefix) && normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            normalized = normalized[prefix.Length..];
        else if (prefix == null && normalized.Contains('-'))
            normalized = normalized[(normalized.LastIndexOf('-') + 1)..];
        return int.TryParse(normalized, out int id) ? id : null;
    }

    private static string NormalizeTab(string? tab)
        => tab?.ToLowerInvariant() is "inventory" or "delivery" ? tab.ToLowerInvariant() : "sales";

    private static string NormalizePeriod(string? period)
        => period?.ToLowerInvariant() is "monthly" or "yearly" ? period.ToLowerInvariant() : "weekly";

    private sealed record ReportLookups(
        IReadOnlyDictionary<int, (string Name, string? CategoryName)> Products,
        IReadOnlyDictionary<int, string> Categories,
        IReadOnlyDictionary<int, string> Customers,
        IReadOnlyDictionary<int, string> CheckoutPaymentTypes)
    {
        public string ProductName(int productId)
            => Products.TryGetValue(productId, out var product) ? product.Name : "Unknown product";

        public string ProductCategoryName(int productId)
            => Products.TryGetValue(productId, out var product)
                ? product.CategoryName ?? "Unknown category"
                : "Unknown category";

        public string CategoryName(int categoryId)
            => Categories.TryGetValue(categoryId, out var name) ? name : "Unknown category";

        public string CustomerName(int customerId)
            => Customers.TryGetValue(customerId, out var name) ? name : "Unknown customer";

        public string CheckoutPaymentType(int checkoutId)
            => CheckoutPaymentTypes.TryGetValue(checkoutId, out var type) ? type : "Unknown";
    }
}