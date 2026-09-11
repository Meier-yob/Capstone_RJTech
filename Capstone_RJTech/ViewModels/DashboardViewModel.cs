using System.Globalization;

namespace Capstone_RJTech.ViewModels;

public sealed class DashboardViewModel
{
    public decimal TotalSales { get; set; }
    public int TotalProducts { get; set; }
    public decimal OutstandingAmount { get; set; }
    public int OutstandingPlans { get; set; }
    public int TotalCustomers { get; set; }
    public int Month { get; set; }
    public int Year { get; set; }
    public int? CategoryId { get; set; }
    public string PeriodLabel => new DateTime(Year, Month, 1).ToString("MMMM yyyy", CultureInfo.InvariantCulture);
    public decimal PeriodSales => SalesOverview.Sum(day => day.Revenue);
    public decimal? SalesChangePercent { get; set; }
    public List<int> Years { get; set; } = [];
    public List<DashboardCategoryViewModel> Categories { get; set; } = [];
    public List<SalesChartItem> SalesOverview { get; set; } = [];
    public List<WeeklySalesChartItem> WeeklySalesOverview { get; set; } = [];
    public InventoryHealthViewModel InventoryHealth { get; set; } = new();
    public List<TopSellingProductViewModel> TopSellingProducts { get; set; } = [];
    public List<CategorySalesViewModel> SalesByCategory { get; set; } = [];
    public List<RecentTransactionViewModel> RecentTransactions { get; set; } = [];
}

public sealed class SalesChartItem
{
    public int Day { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class WeeklySalesChartItem
{
    public DateTime WeekStartDate { get; set; }
    public DateTime WeekEndDate { get; set; }
    public decimal Revenue { get; set; }
    public string Label => $"{WeekStartDate:MMM d}–{WeekEndDate:MMM d}";
}

public sealed class InventoryHealthViewModel
{
    public int Available { get; set; }
    public int Unavailable { get; set; }
    public int LowStock { get; set; }
    public int OutOfStock { get; set; }
    public int Total => Available + Unavailable + LowStock + OutOfStock;
}

public sealed class TopSellingProductViewModel
{
    public int ProductId { get; set; }
    public string ProductCode => Controllers.ProductController.FormatProductCode(Category, ProductId);
    public string Product { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public int QuantitySold { get; set; }
    public decimal Revenue { get; set; }
}

public sealed class DashboardCategoryViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public sealed class CategorySalesViewModel
{
    public int CategoryId { get; set; }
    public string Category { get; set; } = string.Empty;
    public decimal Revenue { get; set; }
}

public sealed class RecentTransactionViewModel
{
    public int CheckoutId { get; set; }
    public string CheckoutCode => $"CHK-{CheckoutId:D3}";
    public string Customer { get; set; } = string.Empty;
    public int Items { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime DatePurchased { get; set; }
}
