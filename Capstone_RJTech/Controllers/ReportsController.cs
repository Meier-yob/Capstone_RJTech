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
    ReportRefreshService reportRefresh,
    ReportUpdateTracker reportUpdates,
    IExcelExportService excelExport) : Controller
{
    private static readonly string[] ReportStatuses = ["Paid", "Ongoing", "Refunded"];
    private static readonly TimeSpan LiveWatchTimeout = TimeSpan.FromSeconds(25);

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
        ViewData["ReportVersion"] = reportUpdates.RefreshedVersion;
        return View(model);
    }

    [HttpGet("/Reports/Watch")]
    public async Task<IActionResult> Watch(long version, CancellationToken cancellationToken)
    {
        long latestVersion = await reportUpdates.WaitForSourceChangeAsync(
            version,
            LiveWatchTimeout,
            cancellationToken);

        return Json(new
        {
            changed = latestVersion != version,
            version = latestVersion
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(
        string? tab,
        string? period,
        int? month,
        int? year,
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken)
    {
        await reportRefresh.RefreshAsync(cancellationToken);
        TempData["ToastSuccess"] = "Report summaries were refreshed successfully.";
        return RedirectToAction(nameof(Index), new { tab, period, month, year, search, status, sort });
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
        await reportRefresh.EnsureGeneratedAsync(cancellationToken);
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

        model.Years = await db.MonthlySales.AsNoTracking().Select(report => report.Year)
            .Concat(db.YearlySales.AsNoTracking().Select(report => report.Year))
            .Distinct().OrderByDescending(value => value).ToListAsync(cancellationToken);
        model.Years = model.Years.Append(today.Year).Append(selectedYear).Distinct().OrderDescending().ToList();

        switch (model.ActiveTab)
        {
            case "inventory":
                model.Inventory = await BuildInventoryAsync(model, cancellationToken);
                break;
            case "delivery":
                model.Delivery = await BuildDeliveryAsync(model, cancellationToken);
                break;
            default:
                model.Sales = await BuildSalesAsync(model, cancellationToken);
                break;
        }

        return model;
    }

    private async Task<SalesOverviewViewModel> BuildSalesAsync(ReportsViewModel filters, CancellationToken cancellationToken)
    {
        var source = await db.SalesOverviews.AsNoTracking()
            .OrderByDescending(report => report.LastUpdated).FirstOrDefaultAsync(cancellationToken);
        var model = source == null ? new SalesOverviewViewModel() : new SalesOverviewViewModel
        {
            TotalSales = source.TotalSales,
            TotalCheckouts = source.TotalTransactions,
            TotalSoldItems = source.TotalItemsSold,
            TotalCustomers = source.TotalCustomers,
            TodaySales = source.TodaySales,
            WeeklySales = source.WeeklySales,
            MonthlySales = source.MonthlySales,
            YearlySales = source.YearlySales,
            LastSaleDate = source.LastSaleDate,
            LastUpdated = source.LastUpdated
        };

        if (filters.Period == "monthly")
        {
            model.History = (await db.MonthlySales.AsNoTracking()
                    .Where(report => report.Year == filters.Year)
                    .OrderBy(report => report.Month).ToListAsync(cancellationToken))
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
            model.History = (await db.YearlySales.AsNoTracking().OrderBy(report => report.Year)
                    .ToListAsync(cancellationToken))
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
            model.History = (await db.WeeklySales.AsNoTracking()
                    .Where(report => report.WeekStartDate < end && report.WeekEndDate >= start)
                    .OrderBy(report => report.WeekStartDate).ToListAsync(cancellationToken))
                .Select(report => new ReportChartPoint
                {
                    Label = $"{report.WeekStartDate:MMM d}–{report.WeekEndDate:MMM d}",
                    Value = report.TotalSales,
                    TotalTransactions = report.TotalTransactions,
                    TotalItems = report.TotalItemsSold
                }).ToList();
        }

        string periodKey = $"{filters.Year:D4}-{filters.Month:D2}";
        var best = await db.BestSellingProducts.AsNoTracking().Include(report => report.Product)
            .ThenInclude(product => product!.Category).FirstOrDefaultAsync(report => report.Period == periodKey, cancellationToken);
        var least = await db.LeastSellingProducts.AsNoTracking().Include(report => report.Product)
            .ThenInclude(product => product!.Category).FirstOrDefaultAsync(report => report.Period == periodKey, cancellationToken);
        model.BestSellingProduct = ToProductSalesRow(best);
        model.LeastSellingProduct = ToProductSalesRow(least);

        model.SalesByCategory = await db.SalesByCategories.AsNoTracking()
            .Where(report => report.Period == periodKey)
            .OrderByDescending(report => report.TotalSalesAmount)
            .Select(report => new CategorySalesReportRow
            {
                CategoryID = report.CategoryID,
                CategoryName = report.Category == null ? "Unknown category" : report.Category.category_name,
                TotalQuantitySold = report.TotalQuantitySold,
                TotalSalesAmount = report.TotalSalesAmount
            }).ToListAsync(cancellationToken);

        var transactions = db.ReportTransactions.AsNoTracking()
            .Where(report => ReportStatuses.Contains(report.Status));
        if (ReportStatuses.Contains(filters.Status ?? string.Empty))
            transactions = transactions.Where(report => report.Status == filters.Status);
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string search = filters.Search;
            int? checkoutId = ParseFormattedId(search, "CHK-");
            transactions = transactions.Where(report =>
                (report.Customer != null && report.Customer.customer_FullName.Contains(search)) ||
                report.PaymentMethod.Contains(search) || report.Status.Contains(search) ||
                (report.Checkout != null && report.Checkout.PaymentType.Contains(search)) ||
                (checkoutId.HasValue && report.CheckoutID == checkoutId.Value));
        }
        transactions = filters.Sort switch
        {
            "amount-asc" => transactions.OrderBy(report => report.Amount),
            "amount-desc" => transactions.OrderByDescending(report => report.Amount),
            "date-asc" => transactions.OrderBy(report => report.TransactionDate),
            _ => transactions.OrderByDescending(report => report.TransactionDate).ThenByDescending(report => report.TransactionID)
        };
        model.RecentTransactions = await transactions.Take(12).Select(report => new TransactionReportRow
        {
            CheckoutID = report.CheckoutID,
            CustomerName = report.Customer == null ? "Unknown customer" : report.Customer.customer_FullName,
            PaymentType = report.Checkout == null ? "Unknown" : report.Checkout.PaymentType,
            PaymentMethod = report.PaymentMethod,
            Amount = report.Amount,
            TransactionDate = report.TransactionDate,
            Status = report.Status
        }).ToListAsync(cancellationToken);
        return model;
    }

    private async Task<InventoryOverviewViewModel> BuildInventoryAsync(ReportsViewModel filters, CancellationToken cancellationToken)
    {
        var source = await db.InventoryOverviews.AsNoTracking()
            .OrderByDescending(report => report.LastUpdated).FirstOrDefaultAsync(cancellationToken);
        var model = source == null ? new InventoryOverviewViewModel() : new InventoryOverviewViewModel
        {
            TotalProducts = source.TotalProducts,
            TotalQuantity = source.TotalQuantity,
            AvailableProducts = source.AvailableProducts,
            UnavailableProducts = source.UnavailableProducts,
            LowStockProducts = source.LowStockProducts,
            OutOfStockProducts = source.OutOfStockProducts,
            LastUpdated = source.LastUpdated
        };

        var stock = db.ProductStockSummaries.AsNoTracking().Include(report => report.Product)
            .ThenInclude(product => product!.Category).AsQueryable();
        if (filters.Status is "Available" or "Unavailable" or "Low Stock" or "Out of Stock")
            stock = stock.Where(report => report.StockStatus == filters.Status);
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string search = filters.Search;
            int? productId = ParseFormattedId(search);
            stock = stock.Where(report =>
                (report.Product != null && report.Product.product_name.Contains(search)) ||
                (report.Product != null && report.Product.Category != null && report.Product.Category.category_name.Contains(search)) ||
                (productId.HasValue && report.ProductID == productId.Value));
        }
        stock = filters.Sort switch
        {
            "quantity-asc" => stock.OrderBy(report => report.CurrentQuantity),
            "quantity-desc" => stock.OrderByDescending(report => report.CurrentQuantity),
            "name" => stock.OrderBy(report => report.Product!.product_name),
            _ => stock.OrderBy(report => report.ProductID)
        };
        var stockSources = await stock.ToListAsync(cancellationToken);
        model.ProductStock = stockSources.Select(report => new ProductStockReportRow
        {
            ProductID = report.ProductID,
            ProductCode = ProductController.FormatProductCode(report.Product?.Category?.category_name, report.ProductID),
            ProductName = report.Product?.product_name ?? "Unknown product",
            CategoryName = report.Product?.Category?.category_name ?? "Unknown category",
            CurrentQuantity = report.CurrentQuantity,
            ReorderLevel = report.ReorderLevel,
            StockStatus = report.StockStatus,
            LastUpdated = report.LastUpdated
        }).ToList();

        model.ProductCategories = await db.ProductCategoryOverviews.AsNoTracking()
            .OrderByDescending(report => report.TotalQuantity)
            .Select(report => new CategoryInventoryReportRow
            {
                CategoryName = report.Category == null ? "Unknown category" : report.Category.category_name,
                TotalProducts = report.TotalProducts,
                TotalQuantity = report.TotalQuantity,
                AvailableQuantity = report.AvailableQuantity,
                UnavailableQuantity = report.UnavailableQuantity,
                LastUpdated = report.LastUpdated
            }).ToListAsync(cancellationToken);

        var most = await db.MostStockedProducts.AsNoTracking().Include(report => report.Product)
            .ThenInclude(product => product!.Category).FirstOrDefaultAsync(cancellationToken);
        var least = await db.LeastStockedProducts.AsNoTracking().Include(report => report.Product)
            .ThenInclude(product => product!.Category).FirstOrDefaultAsync(cancellationToken);
        model.MostStockedProduct = ToStockExtremeRow(most);
        model.LeastStockedProduct = ToStockExtremeRow(least);
        return model;
    }

    private async Task<DeliveryOverviewViewModel> BuildDeliveryAsync(ReportsViewModel filters, CancellationToken cancellationToken)
    {
        var source = await db.DeliveryOverviews.AsNoTracking()
            .OrderByDescending(report => report.LastUpdated).FirstOrDefaultAsync(cancellationToken);
        var model = source == null ? new DeliveryOverviewViewModel() : new DeliveryOverviewViewModel
        {
            TotalDeliveries = source.TotalDeliveries,
            TotalItemsDelivered = source.TotalItemsDelivered,
            TotalProductsDelivered = source.TotalProductsDelivered,
            LastDeliveryDate = source.LastDeliveryDate,
            LastUpdated = source.LastUpdated
        };

        int? deliveryId = ParseFormattedId(filters.Search, "DEL-");
        var deliveries = db.DeliverySummaries.AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(filters.Search) && deliveryId.HasValue)
            deliveries = deliveries.Where(report => report.DeliveryID == deliveryId.Value);
        deliveries = filters.Sort switch
        {
            "items-asc" => deliveries.OrderBy(report => report.TotalItems),
            "items-desc" => deliveries.OrderByDescending(report => report.TotalItems),
            "updated-asc" => deliveries.OrderBy(report => report.LastUpdated),
            _ => deliveries.OrderByDescending(report => report.DeliveryID)
        };
        model.DeliverySummaries = await deliveries.Take(20).Select(report => new DeliverySummaryReportRow
        {
            DeliveryID = report.DeliveryID,
            TotalItems = report.TotalItems,
            TotalProducts = report.TotalProducts,
            LastUpdated = report.LastUpdated
        }).ToListAsync(cancellationToken);

        var productDeliveries = db.ProductDeliverySummaries.AsNoTracking().Include(report => report.Product)
            .ThenInclude(product => product!.Category).AsQueryable();
        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            string search = filters.Search;
            int? productId = ParseFormattedId(search);
            productDeliveries = productDeliveries.Where(report =>
                (report.Product != null && report.Product.product_name.Contains(search)) ||
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
        var productSources = await productDeliveries.Take(20).ToListAsync(cancellationToken);
        model.ProductDeliveries = productSources.Select(report => new ProductDeliveryReportRow
        {
            ProductID = report.ProductID,
            ProductCode = ProductController.FormatProductCode(report.Product?.Category?.category_name, report.ProductID),
            ProductName = report.Product?.product_name ?? "Unknown product",
            TotalQuantityDelivered = report.TotalQuantityDelivered,
            TotalDeliveries = report.TotalDeliveries,
            LastDeliveryDate = report.LastDeliveryDate,
            LastUpdated = report.LastUpdated
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
            new ExcelExportColumn("Stock Status", ExcelColumnType.Status),
            new ExcelExportColumn("Last Updated", ExcelColumnType.Date)
        };
        var rows = model.ProductStock.Select(row => (IReadOnlyList<object?>)
            [row.ProductCode, row.ProductName, row.CategoryName, row.CurrentQuantity, row.ReorderLevel, row.StockStatus, row.LastUpdated]);
        return ExcelFile("Inventory Overview", "ReportsInventory", columns, rows);
    }

    private FileContentResult ExportDelivery(DeliveryOverviewViewModel model)
    {
        var columns = new[]
        {
            new ExcelExportColumn("Product ID"), new ExcelExportColumn("Product Name"),
            new ExcelExportColumn("Total Quantity Delivered", ExcelColumnType.Integer),
            new ExcelExportColumn("Total Deliveries", ExcelColumnType.Integer),
            new ExcelExportColumn("Last Delivery Date", ExcelColumnType.Date),
            new ExcelExportColumn("Last Updated", ExcelColumnType.Date)
        };
        var rows = model.ProductDeliveries.Select(row => (IReadOnlyList<object?>)
            [row.ProductCode, row.ProductName, row.TotalQuantityDelivered, row.TotalDeliveries, row.LastDeliveryDate, row.LastUpdated]);
        return ExcelFile("Delivery Overview", "ReportsDelivery", columns, rows);
    }

    private FileContentResult ExcelFile(
        string title,
        string fileName,
        IReadOnlyList<ExcelExportColumn> columns,
        IEnumerable<IReadOnlyList<object?>> rows)
        => File(excelExport.ExportToExcel(title, columns, rows), ExcelExportFile.ContentType,
            ExcelExportFile.CreateFileName(fileName));

    private static ProductSalesReportRow? ToProductSalesRow(BestSellingProductReport? source)
        => source == null ? null : new ProductSalesReportRow
        {
            ProductID = source.ProductID,
            ProductCode = ProductController.FormatProductCode(source.Product?.Category?.category_name, source.ProductID),
            ProductName = source.Product?.product_name ?? "Unknown product",
            TotalQuantitySold = source.TotalQuantitySold,
            TotalSalesAmount = source.TotalSalesAmount
        };

    private static ProductSalesReportRow? ToProductSalesRow(LeastSellingProductReport? source)
        => source == null ? null : new ProductSalesReportRow
        {
            ProductID = source.ProductID,
            ProductCode = ProductController.FormatProductCode(source.Product?.Category?.category_name, source.ProductID),
            ProductName = source.Product?.product_name ?? "Unknown product",
            TotalQuantitySold = source.TotalQuantitySold,
            TotalSalesAmount = source.TotalSalesAmount
        };

    private static StockExtremeReportRow? ToStockExtremeRow(MostStockedProductReport? source)
        => source == null ? null : new StockExtremeReportRow
        {
            ProductID = source.ProductID,
            ProductCode = ProductController.FormatProductCode(source.Product?.Category?.category_name, source.ProductID),
            ProductName = source.Product?.product_name ?? "Unknown product",
            CurrentQuantity = source.CurrentQuantity,
            LastUpdated = source.LastUpdated
        };

    private static StockExtremeReportRow? ToStockExtremeRow(LeastStockedProductReport? source)
        => source == null ? null : new StockExtremeReportRow
        {
            ProductID = source.ProductID,
            ProductCode = ProductController.FormatProductCode(source.Product?.Category?.category_name, source.ProductID),
            ProductName = source.Product?.product_name ?? "Unknown product",
            CurrentQuantity = source.CurrentQuantity,
            LastUpdated = source.LastUpdated
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
}
