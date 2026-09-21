using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Capstone_RJTech.Models;

[Table("tblSalesOverview")]
public sealed class SalesOverviewReport 
{
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
    public int TotalCustomers { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TodaySales { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal WeeklySales { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal MonthlySales { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal YearlySales { get; set; }
    public DateTime? LastSaleDate { get; set; }

    [Key]
    public DateTime LastUpdated { get; set; }
}

[Table("tblWeeklySales")]
public sealed class WeeklySalesReport 
{
    [Key]
    public int WeeklySalesID { get; set; }
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
}

[Table("tblMonthlySales")]
public sealed class MonthlySalesReport 
{
    [Key]
    public int MonthlySalesID { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
}

[Table("tblYearlySales")]
public sealed class YearlySalesReport 
{
    [Key]
    public int YearlySalesID { get; set; }
    public int Year { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSales { get; set; }
    public int TotalTransactions { get; set; }
    public int TotalItemsSold { get; set; }
}

[Table("tblBestSellingProduct")]
public sealed class BestSellingProductReport 
{
    [Key]
    public int BestSellingID { get; set; }
    public int ProductID { get; set; }
    public int TotalQuantitySold { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSalesAmount { get; set; }

    [Required, StringLength(20)]
    public string Period { get; set; } = string.Empty;

    public DateTime DateGenerated { get; set; }
    public Product? Product { get; set; }
}

[Table("tblLeastSellingProduct")]
public sealed class LeastSellingProductReport 
{
    [Key]
    public int LeastSellingID { get; set; }
    public int ProductID { get; set; }
    public int TotalQuantitySold { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSalesAmount { get; set; }

    [Required, StringLength(20)]
    public string Period { get; set; } = string.Empty;

    public DateTime DateGenerated { get; set; }
    public Product? Product { get; set; }
}

[Table("tblSalesByCategory")]
public sealed class SalesByCategoryReport 
{
    [Key]
    public int SalesCategoryID { get; set; }
    public int CategoryID { get; set; }
    public int TotalQuantitySold { get; set; }
    [Column(TypeName = "decimal(18,2)")]
    public decimal TotalSalesAmount { get; set; }

    [Required, StringLength(20)]
    public string Period { get; set; } = string.Empty;

    public DateTime DateGenerated { get; set; }
    public ProductCategory? Category { get; set; }
}

[Table("tblTransaction")]
public sealed class TransactionReport 
{
    [Key]
    public int TransactionID { get; set; }
    public int CheckoutID { get; set; }
    public int CustomerID { get; set; }

    [Required, StringLength(30)]
    public string PaymentType { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string PaymentMethod { get; set; } = string.Empty;

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }
    public DateTime TransactionDate { get; set; }

    [Required, StringLength(30)]
    public string Status { get; set; } = string.Empty;

    public Checkout? Checkout { get; set; }
    public Customer? Customer { get; set; }
}

[Table("tblInventoryOverview")]
public sealed class InventoryOverviewReport 
{
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public int AvailableProducts { get; set; }
    public int UnavailableProducts { get; set; }
    public int LowStockProducts { get; set; }
    public int OutOfStockProducts { get; set; }

    [Key]
    public DateTime LastUpdated { get; set; }
}

[Table("tblProductStockSummary")]
public sealed class ProductStockSummaryReport 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ProductID { get; set; }
    public int CurrentQuantity { get; set; }
    public int ReorderLevel { get; set; }

    [Required, StringLength(30)]
    public string StockStatus { get; set; } = string.Empty;

    public DateTime LastUpdated { get; set; }
    public Product? Product { get; set; }
}

[Table("tblProductCategoryOverview")]
public sealed class ProductCategoryOverviewReport 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int CategoryID { get; set; }
    public int TotalProducts { get; set; }
    public int TotalQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public int UnavailableQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
    public ProductCategory? Category { get; set; }
}

[Table("tblMostStockedProduct")]
public sealed class MostStockedProductReport 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ProductID { get; set; }
    public int CurrentQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
    public Product? Product { get; set; }
}

[Table("tblLeastStockedProduct")]
public sealed class LeastStockedProductReport 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ProductID { get; set; }
    public int CurrentQuantity { get; set; }
    public DateTime LastUpdated { get; set; }
    public Product? Product { get; set; }
}

[Table("tblDeliveryOverview")]
public sealed class DeliveryOverviewReport 
{
    public int TotalDeliveries { get; set; }
    public int TotalItemsDelivered { get; set; }
    public int TotalProductsDelivered { get; set; }
    public DateTime? LastDeliveryDate { get; set; }

    [Key]
    public DateTime LastUpdated { get; set; }
}

[Table("tblDeliverySummary")]
public sealed class DeliverySummaryReport 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int DeliveryID { get; set; }
    public int TotalItems { get; set; }
    public int TotalProducts { get; set; }
    public DateTime LastUpdated { get; set; }
    public Delivery? Delivery { get; set; }
}

[Table("tblProductDeliverySummary")]
public sealed class ProductDeliverySummaryReport 
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ProductID { get; set; }
    public int TotalQuantityDelivered { get; set; }
    public int TotalDeliveries { get; set; }
    public DateTime? LastDeliveryDate { get; set; }
    public DateTime LastUpdated { get; set; }
    public Product? Product { get; set; }
}
