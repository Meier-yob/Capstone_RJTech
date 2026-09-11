using Capstone_RJTech.Controllers;
using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

// SQL Server integration checks against a unique disposable LocalDB database.
// The application's RJTechInventory database is never used by this executable.
var databaseName = "RJTechDashboardTest_" + Guid.NewGuid().ToString("N");
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True")
    .Options;
await using var db = new ApplicationDbContext(options);
try
{
    await db.Database.EnsureCreatedAsync();
    db.Products.RemoveRange(db.Products);
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();
    var controller = new DashboardController(db);
    async Task<DashboardViewModel> Load(int month = 2, int year = 2024, int? category = null)
        => (DashboardViewModel)((ViewResult)await controller.Index(month, year, category, default)).Model!;
    void Check(bool condition, string description)
    {
        if (!condition) throw new InvalidOperationException(description);
        Console.WriteLine("PASS " + description);
    }

    var empty = await Load();
    Check(empty.TotalSales == 0 && empty.OutstandingAmount == 0 && empty.TotalProducts == 0 &&
        empty.RecentTransactions.Count == 0 && empty.TopSellingProducts.Count == 0 && empty.SalesByCategory.Count == 0,
        "Empty database produces zero totals and empty panels.");

    var products = new[]
    {
        new Product { product_name = "Available test", product_brand = "Test", category_ID = 1, product_quantity = 11, reorder_level = 5, product_status = "Low Stock", Product_price = 9999 },
        new Product { product_name = "Unavailable test", product_brand = "Test", category_ID = 2, product_quantity = 100, reorder_level = 5, product_status = "Unavailable", Product_price = 9999 },
        new Product { product_name = "Reorder boundary", product_brand = "Test", category_ID = 3, product_quantity = 5, reorder_level = 5, product_status = "Available", Product_price = 9999 },
        new Product { product_name = "Out of stock test", product_brand = "Test", category_ID = 4, product_quantity = 0, reorder_level = 5, product_status = "Available", Product_price = 9999 }
    };
    db.Products.AddRange(products);
    var customer = new Customer { customer_FullName = "Dashboard test", customer_Email = "dashboard-test@gmail.com", customer_Phone = "09123456789", customer_Address = "Test address" };
    Checkout Sale(DateTime date, string status, decimal amount, Product product, int quantity = 1, string? planStatus = null, decimal balance = 0)
    {
        var sale = new Checkout
        {
            Customer = customer, DatePurchased = date, Status = status, TotalAmount = amount,
            PaymentType = planStatus == null ? "Full Payment" : "Installment", PaymentMethod = "Cash",
            CheckoutItems = [new CheckoutItem { Product = product, ItemQuantity = quantity, Price = amount / quantity, SubTotal = amount }]
        };
        if (planStatus != null) sale.Installment = new Installment { Status = planStatus, Balance = balance, TotalAmount = balance, Months = 6 };
        return sale;
    }
    db.Checkouts.AddRange(
        Sale(new(2024, 1, 31, 23, 59, 59), "Paid", 50, products[0]),
        Sale(new(2024, 2, 1), "Paid", 200, products[0], quantity: 2),
        Sale(new(2024, 2, 29, 23, 59, 59), "Paid", 300, products[1], planStatus: "Completed"),
        Sale(new(2024, 3, 1), "Paid", 500, products[0]),
        Sale(new(2024, 2, 12), "Ongoing", 999, products[0], planStatus: "Active", balance: 120),
        Sale(new(2024, 2, 13), "Ongoing", 777, products[0], planStatus: "Overdue", balance: 80),
        Sale(new(2024, 2, 20), "Cancelled", 800, products[0], planStatus: "Active", balance: 400),
        Sale(new(2024, 2, 21), "Refunded", 900, products[0], planStatus: "Overdue", balance: 500));
    await db.SaveChangesAsync();
    db.ChangeTracker.Clear();

    var result = await Load();
    Check(result.TotalSales == 1050 && result.PeriodSales == 500, "Only paid checkouts count toward all-time and selected-month revenue.");
    Check(result.OutstandingAmount == 200 && result.OutstandingPlans == 2, "Outstanding balances exclude completed, cancelled and refunded checkouts.");
    Check(result.TotalProducts == 4 && result.InventoryHealth.Available == 1 && result.InventoryHealth.Unavailable == 1 &&
        result.InventoryHealth.LowStock == 1 && result.InventoryHealth.OutOfStock == 1, "Existing inventory rules handle unavailable products and reorder/zero boundaries.");
    Check(result.SalesOverview.Count == 29 && result.SalesOverview[0].Revenue == 200 && result.SalesOverview[28].Revenue == 300 &&
        result.SalesOverview[1].Revenue == 0 && result.SalesChangePercent == 900, "Leap years, zero-filled days and month boundaries aggregate correctly.");
    Check(result.WeeklySalesOverview.First().WeekStartDate == new DateTime(2024, 1, 29) &&
        result.WeeklySalesOverview.First().WeekEndDate == new DateTime(2024, 2, 4) && result.WeeklySalesOverview.First().Revenue == 250 &&
        result.WeeklySalesOverview.Last().WeekStartDate == new DateTime(2024, 2, 26) && result.WeeklySalesOverview.Last().Revenue == 800,
        "Dashboard weekly sales use the same Monday-to-Sunday boundaries as reports, including boundary dates.");
    Check(result.TopSellingProducts.Count == 2 && result.TopSellingProducts[0].ProductId == products[0].product_ID &&
        result.TopSellingProducts[0].QuantitySold == 2 && result.TopSellingProducts[0].Revenue == 200,
        "Product rankings use sold quantities and historical subtotals, not current prices.");
    Check(result.SalesByCategory.Sum(category => category.Revenue) == 500, "Category revenue matches paid sale details.");
    Check(result.RecentTransactions.Count == 5 && result.RecentTransactions[0].DatePurchased == new DateTime(2024, 3, 1),
        "Recent transactions remain store-wide and sorted newest first.");
    var filtered = await Load(category: 1);
    Check(filtered.SalesByCategory.Count == 1 && filtered.SalesByCategory[0].Revenue == 200 && filtered.PeriodSales == 500,
        "Category selection filters only the category panel.");
    var ajax = (JsonResult)await controller.CategorySales(2, 2024, 1, default);
    Check(((List<CategorySalesViewModel>)ajax.Value!).Single().Revenue == 200, "AJAX category endpoint matches the page totals.");
    Check(await controller.CategorySales(2, 2024, int.MaxValue, default) is BadRequestObjectResult,
        "Invalid categories return a validation response.");
    Check((await Load(0, -1, int.MaxValue)).CategoryId == null && (await Load(2, 2023)).SalesOverview.Count == 28,
        "Invalid filters normalize safely and non-leap February has 28 days.");
    Check((await Load(12, 9998)).PeriodSales == 0 && (await Load(1, 1900)).PeriodSales == 0,
        "Supported year boundaries and empty periods do not throw.");
    Check(!db.ChangeTracker.Entries().Any() &&
        await db.Products.AsNoTracking().Where(product => product.product_ID == products[0].product_ID).Select(product => product.product_status).SingleAsync() == "Low Stock" &&
        await db.Installments.AsNoTracking().Where(plan => plan.Status == "Active").SumAsync(plan => plan.Balance) == 520,
        "Dashboard reads do not track or modify product and installment records.");
    Console.WriteLine("All dashboard SQL Server integration checks passed.");
}
finally
{
    // Only the exact database generated above can be removed.
    if (db.Database.GetDbConnection().Database != databaseName || !databaseName.StartsWith("RJTechDashboardTest_"))
        throw new InvalidOperationException("Refusing to clean up an unexpected database.");
    await db.Database.EnsureDeletedAsync();
}
