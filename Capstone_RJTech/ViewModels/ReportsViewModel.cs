namespace Capstone_RJTech.ViewModels;

public sealed class ReportsViewModel
{
    public string ActiveTab { get; set; } = "sales";
    public string Period { get; set; } = "weekly";
    public int Month { get; set; }
    public int Year { get; set; }
    public string? Search { get; set; }
    public string? Status { get; set; }
    public string? Sort { get; set; }
    public List<int> Years { get; set; } = [];

    public SalesOverviewViewModel Sales { get; set; } = new();
    public InventoryOverviewViewModel Inventory { get; set; } = new();
    public DeliveryOverviewViewModel Delivery { get; set; } = new();
}

public sealed class SalesOverviewViewModel
{
    public decimal TotalSales { get; set; }
    public int TotalCheckouts { get; set; }
    public int TotalSoldItems { get; set; }
    public int TotalCustomers { get; set; }
    public decimal TodaySales { get; set; }
    public decimal WeeklySales { get; set; }
    public decimal MonthlySales { get; set; }
    public decimal YearlySales { get; set; }
    public DateTime? LastSaleDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public List<ReportChartPoint> History { get; set; } = [];
    public ProductSalesReportRow? BestSellingProduct { get; set; }
    public ProductSalesReportRow? LeastSellingProduct { get; set; }
    public List<CategorySalesReportRow> SalesByCategory { get; set; } = [];
    public List<TransactionReportRow> RecentTransactions { get; set; } = [];
}

public sealed class InventoryOverviewViewModel
{
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public int AvailableProducts { get; set; }
    public int UnavailableProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
    public DateTime? LastUpdated { get; set; }
    public List<ProductStockReportRow> ProductStock { get; set; } = [];
    public List<CategoryInventoryReportRow> ProductCategories { get; set; } = [];
    public StockExtremeReportRow? MostStockedProduct { get; set; }
    public StockExtremeReportRow? LeastStockedProduct { get; set; }
}

public sealed class DeliveryOverviewViewModel
{
    public int TotalDeliveries { get; set; }
    public int TotalItemsDelivered { get; set; }
    public int TotalProductsDelivered { get; set; }
    public DateTime? LastDeliveryDate { get; set; }
    public DateTime? LastUpdated { get; set; }
    public List<DeliverySummaryReportRow> DeliverySummaries { get; set; } = [];
    public List<ProductDeliveryReportRow> ProductDeliveries { get; set; } = [];
}

public sealed class ReportChartPoint
{
    public string Label { get; set; } = string.Empty;
    public decimal Value { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItems { get; set; }
}

public sealed class ProductSalesReportRow
{
    public int ProductID { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
}

public sealed class CategorySalesReportRow
{
    public int CategoryID { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public int TotalQuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
}

public sealed class TransactionReportRow
{
    public int CheckoutID { get; set; }
    public string CheckoutCode => $"CHK-{CheckoutID:D3}";
    public string CustomerName { get; set; } = string.Empty;
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class ProductStockReportRow
{
    public int ProductID { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public string StockStatus { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
}

public sealed class CategoryInventoryReportRow
{
    public string CategoryName { get; set; } = string.Empty;
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int UnavailableQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class StockExtremeReportRow
{
    public int ProductID { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int CurrentQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class DeliverySummaryReportRow
{
    public int DeliveryID { get; set; }
    public string DeliveryCode => $"DEL-{DeliveryID:D3}";
    public int TotalItems { get; set; }
    public int TotalProducts { get; set; }
    public DateTime LastUpdated { get; set; }
}

public sealed class ProductDeliveryReportRow
{
    public int ProductID { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int TotalQuantityDelivered { get; set; }
    public int TotalDeliveries { get; set; }
    public DateTime? LastDeliveryDate { get; set; }
    public DateTime LastUpdated { get; set; }
}
