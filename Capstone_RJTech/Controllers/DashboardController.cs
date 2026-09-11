using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class DashboardController(ApplicationDbContext db) : Controller
{
    [HttpGet("/Dashboard")]
    [HttpGet("/Dashboard/Index")]
    public async Task<IActionResult> Index(int? month, int? year, int? categoryId, CancellationToken cancellationToken)
    {
        var start = SelectedPeriod(month, year);
        var end = start.AddMonths(1);
        var previousStart = start.AddMonths(-1);
        var weeklyStart = SalesPeriod.StartOfWeek(start);
        var weeklyEnd = SalesPeriod.StartOfWeek(end.AddDays(-1)).AddDays(7);
        var model = new DashboardViewModel { Month = start.Month, Year = start.Year };

        // Paid is the existing checkout status for a completed sale. Ongoing,
        // cancelled and refunded checkouts are excluded from every revenue panel.
        var paidCheckouts = db.Checkouts.AsNoTracking().Where(checkout => checkout.Status == "Paid");
        model.TotalSales = await paidCheckouts.SumAsync(checkout => (decimal?)checkout.TotalAmount, cancellationToken) ?? 0;
        model.TotalCustomers = await db.Customers.AsNoTracking().CountAsync(cancellationToken);

        var outstanding = await db.Installments.AsNoTracking()
            .Where(plan => (plan.Status == "Active" || plan.Status == "Overdue") && plan.Balance > 0 &&
                plan.Checkout != null && plan.Checkout.Status == "Ongoing")
            .GroupBy(plan => 1)
            .Select(group => new { Amount = group.Sum(plan => plan.Balance), Count = group.Count() })
            .SingleOrDefaultAsync(cancellationToken);
        model.OutstandingAmount = outstanding?.Amount ?? 0;
        model.OutstandingPlans = outstanding?.Count ?? 0;

        // Group only the fields required by the existing rule; never load images,
        // track products, or synchronize inventory/installments on a dashboard GET.
        var inventoryGroups = await db.Products.AsNoTracking()
            .GroupBy(product => new { product.product_status, product.product_quantity, product.reorder_level })
            .Select(group => new
            {
                group.Key.product_status, group.Key.product_quantity, group.Key.reorder_level,
                Count = group.Count()
            }).ToListAsync(cancellationToken);
        foreach (var group in inventoryGroups)
        {
            var status = ProductController.EvaluateProductStatus(new Product
            {
                product_status = group.product_status,
                product_quantity = group.product_quantity,
                reorder_level = group.reorder_level
            });
            switch (status)
            {
                case "Available": model.InventoryHealth.Available += group.Count; break;
                case "Unavailable": model.InventoryHealth.Unavailable += group.Count; break;
                case "Low Stock": model.InventoryHealth.LowStock += group.Count; break;
                case "Out of Stock": model.InventoryHealth.OutOfStock += group.Count; break;
            }
        }
        model.TotalProducts = model.InventoryHealth.Total;

        var dailySales = await paidCheckouts
            .Where(checkout => checkout.DatePurchased >= previousStart && checkout.DatePurchased < weeklyEnd)
            .GroupBy(checkout => checkout.DatePurchased.Date)
            .Select(group => new { Date = group.Key, Revenue = group.Sum(checkout => checkout.TotalAmount) })
            .ToListAsync(cancellationToken);
        var selectedDays = dailySales.Where(day => day.Date >= start && day.Date < end)
            .ToDictionary(day => day.Date.Day, day => day.Revenue);
        model.SalesOverview = Enumerable.Range(1, DateTime.DaysInMonth(start.Year, start.Month))
            .Select(day => new SalesChartItem { Day = day, Revenue = selectedDays.GetValueOrDefault(day) }).ToList();
        model.WeeklySalesOverview = dailySales
            .Where(day => day.Date >= weeklyStart && day.Date < weeklyEnd)
            .GroupBy(day => SalesPeriod.StartOfWeek(day.Date))
            .OrderBy(group => group.Key)
            .Select(group => new WeeklySalesChartItem
            {
                WeekStartDate = group.Key,
                WeekEndDate = group.Key.AddDays(6),
                Revenue = group.Sum(day => day.Revenue)
            }).ToList();
        var previousSales = dailySales.Where(day => day.Date >= previousStart && day.Date < start)
            .Sum(day => day.Revenue);
        model.SalesChangePercent = previousSales > 0
            ? Math.Round((model.PeriodSales - previousSales) / previousSales * 100, 1)
            : null;

        model.Years = await db.Checkouts.AsNoTracking().Select(checkout => checkout.DatePurchased.Year)
            .Distinct().ToListAsync(cancellationToken);
        model.Years = model.Years.Append(DateTime.Today.Year).Append(start.Year)
            .Where(value => value is >= 1900 and <= 9998).Distinct().OrderDescending().ToList();
        model.Categories = await db.ProductCategories.AsNoTracking().OrderBy(category => category.category_name)
            .Select(category => new DashboardCategoryViewModel { Id = category.category_ID, Name = category.category_name })
            .ToListAsync(cancellationToken);
        model.CategoryId = model.Categories.Any(category => category.Id == categoryId) ? categoryId : null;

        var periodItems = PaidItems(start, end);
        model.TopSellingProducts = await periodItems
            .GroupBy(item => new { item.ProductID, item.Product!.product_name, item.Product.Category!.category_name })
            .Select(group => new TopSellingProductViewModel
            {
                ProductId = group.Key.ProductID,
                Product = group.Key.product_name,
                Category = group.Key.category_name,
                QuantitySold = group.Sum(item => item.ItemQuantity),
                Revenue = group.Sum(item => item.SubTotal)
            }).OrderByDescending(product => product.QuantitySold).ThenByDescending(product => product.Revenue)
            .ThenBy(product => product.ProductId).Take(5).ToListAsync(cancellationToken);

        model.SalesByCategory = await CategoryRevenue(start, end, model.CategoryId).ToListAsync(cancellationToken);
        // The latest activity is intentionally store-wide, independent of chart filters.
        model.RecentTransactions = await db.Checkouts.AsNoTracking()
            .OrderByDescending(checkout => checkout.DatePurchased).ThenByDescending(checkout => checkout.CheckoutID)
            .Take(5).Select(checkout => new RecentTransactionViewModel
            {
                CheckoutId = checkout.CheckoutID,
                Customer = checkout.Customer != null ? checkout.Customer.customer_FullName : "Unknown customer",
                Items = checkout.CheckoutItems.Sum(item => item.ItemQuantity),
                PaymentMethod = checkout.PaymentMethod,
                TotalAmount = checkout.TotalAmount,
                Status = checkout.Status,
                DatePurchased = checkout.DatePurchased
            }).ToListAsync(cancellationToken);
        return View(model);
    }

    [HttpGet]
    public async Task<IActionResult> CategorySales(int? month, int? year, int? categoryId, CancellationToken cancellationToken)
    {
        var start = SelectedPeriod(month, year);
        if (categoryId.HasValue && !await db.ProductCategories.AsNoTracking()
                .AnyAsync(category => category.category_ID == categoryId, cancellationToken))
            return BadRequest(new { message = "Select an existing product category." });
        return Json(await CategoryRevenue(start, start.AddMonths(1), categoryId).ToListAsync(cancellationToken));
    }

    private IQueryable<CheckoutItem> PaidItems(DateTime start, DateTime end)
        => db.CheckoutItems.AsNoTracking().Where(item => item.Checkout != null && item.Checkout.Status == "Paid" &&
            item.Checkout.DatePurchased >= start && item.Checkout.DatePurchased < end);

    private IQueryable<CategorySalesViewModel> CategoryRevenue(DateTime start, DateTime end, int? categoryId)
    {
        var items = PaidItems(start, end);
        if (categoryId.HasValue) items = items.Where(item => item.Product!.category_ID == categoryId);
        return items.GroupBy(item => new { item.Product!.category_ID, item.Product.Category!.category_name })
            .Select(group => new CategorySalesViewModel
            {
                CategoryId = group.Key.category_ID,
                Category = group.Key.category_name,
                Revenue = group.Sum(item => item.SubTotal)
            }).OrderByDescending(category => category.Revenue).ThenBy(category => category.Category);
    }

    private static DateTime SelectedPeriod(int? month, int? year)
        => new(year is >= 1900 and <= 9998 ? year.Value : DateTime.Today.Year,
            month is >= 1 and <= 12 ? month.Value : DateTime.Today.Month, 1);
}
