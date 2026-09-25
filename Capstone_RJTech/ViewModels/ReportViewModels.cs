namespace Capstone_RJTech.ViewModels;

/// <summary>
/// Report data sets. These are computed on demand from the live inventory and
/// sales tables (see <c>ReportComputationService</c>) and bound directly to the
/// Reports page — they are view models, not persisted entities.
/// </summary>

public sealed class SalesOverviewReport
{
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
    public int TotalCustomers { get; set; }
    public decimal TodaySales { get; set; }
    public decimal WeeklySales { get; set; }
    public decimal MonthlySales { get; set; }
    public decimal YearlySales { get; set; }
    public DateTime? LastSaleDate { get; set; }
}

public sealed class WeeklySalesReport
{
    public int WeeklySalesID { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
}

public sealed class MonthlySalesReport
{
    public int MonthlySalesID { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
}

public sealed class YearlySalesReport
{
    public int YearlySalesID { get; set; }
    public int Year { get; set; }
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
}

public sealed class BestSellingProductReport
{
    public int BestSellingID { get; set; }
    public int ProductID { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime DateGenerated { get; set; }
}

public sealed class LeastSellingProductReport
{
    public int LeastSellingID { get; set; }
    public int ProductID { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime DateGenerated { get; set; }
}

public sealed class SalesByCategoryReport
{
    public int SalesCategoryID { get; set; }
    public int CategoryID { get; set; }
    public int TotalQuantitySold { get; set; }
    public decimal TotalSalesAmount { get; set; }
    public string Period { get; set; } = string.Empty;
    public DateTime DateGenerated { get; set; }
}

public sealed class TransactionReport
{
    public int TransactionID { get; set; }
    public int CheckoutID { get; set; }
    public int CustomerID { get; set; }
    public string PaymentType { get; set; } = string.Empty;
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }
    public string Status { get; set; } = string.Empty;
}

public sealed class InventoryOverviewReport
{
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public int AvailableProducts { get; set; }
    public int UnavailableProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
}

public sealed class ProductStockSummaryReport
{
    public int ProductID { get; set; }
    public int CurrentQuantity { get; set; }
    public int ReorderLevel { get; set; }
    public string StockStatus { get; set; } = string.Empty;
}

public sealed class ProductCategoryOverviewReport
{
    public int CategoryID { get; set; }
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int UnavailableQuantity { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }
}

public sealed class MostStockedProductReport
{
    public int ProductID { get; set; }
    public int CurrentQuantity { get; set; }
}

public sealed class LeastStockedProductReport
{
    public int ProductID { get; set; }
    public int CurrentQuantity { get; set; }
}

public sealed class DeliveryOverviewReport
{
    public int TotalDeliveries { get; set; }
    public int TotalItemsDelivered { get; set; }
    public int TotalProductsDelivered { get; set; }
    public DateTime? LastDeliveryDate { get; set; }
}

public sealed class DeliverySummaryReport
{
    public int DeliveryID { get; set; }
    public int TotalItems { get; set; }
    public int TotalProducts { get; set; }
}

public sealed class ProductDeliverySummaryReport
{
    public int ProductID { get; set; }
    public int TotalQuantityDelivered { get; set; }
    public int TotalDeliveries { get; set; }
    public DateTime? LastDeliveryDate { get; set; }
}