using System.Text.Json;
using Capstone_RJTech.Controllers;
using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Capstone_RJTech.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Logging.Abstractions;

// Only this unique disposable database is used, never the configured application database.
string databaseName = "RJTechSalesHistoryTest_" + Guid.NewGuid().ToString("N");
int instanceOption = Array.IndexOf(args, "--instance");
string instanceName = instanceOption >= 0 && instanceOption + 1 < args.Length
    ? args[instanceOption + 1]
    : "MSSQLLocalDB";
var options = new DbContextOptionsBuilder<ApplicationDbContext>()
    .UseSqlServer($"Server=(localdb)\\{instanceName};Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True")
    .Options;
await using var db = new ApplicationDbContext(options);
bool keepPreview = false;
void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    Console.WriteLine("PASS " + description);
}
bool Success(IActionResult action)
    => JsonSerializer.SerializeToElement(((JsonResult)action).Value).GetProperty("success").GetBoolean();

try
{
    await db.GetService<IMigrator>().MigrateAsync("20260906102343_RemoveScheduleEvents");
    var customer = new Customer
    {
        customer_FullName = "Maria Santos", customer_Email = "maria@gmail.com",
        customer_Phone = "09123456789", customer_Address = "Manila"
    };
    var legacyFull = new Checkout
    {
        Customer = customer, TotalAmount = 1200, PaymentMethod = "Cash",
        DatePurchased = new DateTime(2026, 7, 1, 10, 30, 0)
    };
    var legacyPlan = new Checkout
    {
        Customer = customer, TotalAmount = 1000, PaymentType = "Installment", Status = "Ongoing",
        PaymentMethod = "Bank Transfer", DatePurchased = new DateTime(2026, 7, 2),
        Installment = new Installment
        {
            DownPayment = 200, TotalAmount = 1040, Balance = 740, MonthlyPayment = 173.33m,
            InterestRate = 5, Months = 6, StartDate = new DateTime(2026, 7, 2),
            InstallmentPayments =
            [
                new() { PaymentAmount = 300, PaymentMethod = "E-Wallet", PaymentDate = new DateTime(2026, 8, 2) },
                new() { PaymentAmount = 77, Status = "Pending", PaymentDate = new DateTime(2026, 8, 3) }
            ]
        }
    };
    var legacyRefund = new Checkout
    {
        Customer = customer, TotalAmount = 100, Status = "Refunded", DatePurchased = new DateTime(2026, 7, 3)
    };
    db.Checkouts.AddRange(legacyFull, legacyPlan, legacyRefund);
    await db.SaveChangesAsync();
    await db.Database.MigrateAsync();
    db.ChangeTracker.Clear();
    var imported = await db.CustomerPurchaseHistories.OrderBy(row => row.HistoryID).ToListAsync();
    Check(imported.Count == 4 && imported.Sum(row => row.TotalAmount) == 1800,
        "Migration imports full receipts, down payments, paid instalments and refunded-order receipts without counting unpaid entries.");
    Check(imported[0].FormattedHistoryID == "PAY-001" && imported[^1].TotalAmount == 300 &&
        imported[^1].PaymentMethod == "E-Wallet", "Imported IDs are chronological and individual payment methods are preserved.");
    await db.Database.MigrateAsync();
    Check(await db.CustomerPurchaseHistories.CountAsync() == 4, "Running migration again does not duplicate history.");
    Check(db.Model.FindEntityType(typeof(CustomerPurchaseHistory))!.GetProperties().Count() == 6,
        "History table maps exactly the six requested columns.");
    Check(new CustomerPurchaseHistory { HistoryID = 1000 }.FormattedHistoryID == "PAY-1000",
        "PAY identifiers continue beyond three digits.");

    var product = await db.Products.FirstAsync();
    product.product_quantity = 20;
    product.Product_price = 18000;
    product.product_status = "Available";
    // This check exercises serialized-inventory duplicate protection explicitly.
    product.is_serialized = true;
    await db.SaveChangesAsync();
    var installments = new InstallmentService(db, NullLogger<InstallmentService>.Instance);
    var sales = new SalesController(db, NullLogger<SalesController>.Instance, new StockNotificationService(db), installments)
    { Url = new TestUrlHelper() };
    SaveCheckoutRequest Request(string serial, string type = "Full Payment") => new()
    {
        CustomerFullName = "Juan Dela Cruz", CustomerEmail = "juan@gmail.com",
        CustomerPhone = "09987654321", CustomerAddress = "Quezon City",
        PaymentType = type, PaymentMethod = "Cash", InstallmentMonths = type == "Installment" ? 6 : null,
        Items = [new() { ProductID = product.product_ID, Quantity = 1, SerialNumbers = [serial] }]
    };
    Check(Success(sales.CompleteCheckout(Request("HISTORY-FULL"))), "Full checkout completes successfully.");
    var full = await db.Checkouts.OrderByDescending(row => row.CheckoutID).FirstAsync();
    Check(await db.CustomerPurchaseHistories.CountAsync(row => row.CheckoutID == full.CheckoutID) == 1 &&
        (await db.CustomerPurchaseHistories.SingleAsync(row => row.CheckoutID == full.CheckoutID)).TotalAmount == 18000,
        "Full checkout records exactly one payment for its total.");
    Check(!Success(sales.CompleteCheckout(Request("HISTORY-FULL"))) && await db.CustomerPurchaseHistories.CountAsync() == 5,
        "Resubmitting an already-sold serial does not create a duplicate receipt.");
    Check(Success(sales.CompleteCheckout(Request("HISTORY-PLAN", "Installment"))), "Instalment checkout completes successfully.");
    var plan = await db.Installments.OrderByDescending(row => row.InstallmentID).FirstAsync();
    Check((await db.CustomerPurchaseHistories.SingleAsync(row => row.CheckoutID == plan.CheckoutID)).TotalAmount == 3600,
        "Instalment checkout records only the 20% down payment, not the sale or financed total.");

    var partial = await installments.RecordPaymentAsync(plan.InstallmentID, 1000, "E-Wallet");
    Check(partial.Success && await db.CustomerPurchaseHistories.CountAsync(row => row.CheckoutID == plan.CheckoutID) == 2,
        "Partial instalment payment appends one receipt.");
    var receipt = await db.CustomerPurchaseHistories.OrderByDescending(row => row.HistoryID).FirstAsync();
    var source = await db.InstallmentPayments.OrderByDescending(row => row.PaymentID).FirstAsync();
    Check(receipt.TotalAmount == source.PaymentAmount && receipt.PurchaseDate == source.PaymentDate &&
        receipt.PaymentMethod == source.PaymentMethod && receipt.CustomerID == full.CustomerID,
        "Receipt preserves the actual payment amount, timestamp, method and customer.");
    int countBefore = await db.CustomerPurchaseHistories.CountAsync();
    Check(!(await installments.RecordPaymentAsync(plan.InstallmentID, 0, "Cash")).Success &&
        !(await installments.RecordPaymentAsync(plan.InstallmentID, 999999, "Cash")).Success &&
        !(await installments.RecordPaymentAsync(plan.InstallmentID, 10, "Invalid")).Success &&
        await db.CustomerPurchaseHistories.CountAsync() == countBefore,
        "Invalid, zero and excessive payments do not create history.");

    decimal balanceBefore = plan.Balance;
    int paymentsBefore = await db.InstallmentPayments.CountAsync();
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE [tblCustomerPurchaseHistory] ADD CONSTRAINT [TestFailReceipt] CHECK ([TotalAmount] <> 123)");
    Check(!(await installments.RecordPaymentAsync(plan.InstallmentID, 123, "Cash")).Success,
        "A receipt storage failure rejects the payment.");
    db.ChangeTracker.Clear();
    Check(await db.InstallmentPayments.CountAsync() == paymentsBefore &&
        await db.CustomerPurchaseHistories.CountAsync() == countBefore &&
        (await db.Installments.FindAsync(plan.InstallmentID))!.Balance == balanceBefore,
        "Failed receipt rolls back its payment and balance update together.");
    await db.Database.ExecuteSqlRawAsync("ALTER TABLE [tblCustomerPurchaseHistory] DROP CONSTRAINT [TestFailReceipt]");

    Check((await installments.RecordPaymentAsync(plan.InstallmentID, balanceBefore, "Bank Transfer")).Success,
        "Final instalment payment completes the plan.");
    var planReceipts = await db.CustomerPurchaseHistories.Where(row => row.CheckoutID == plan.CheckoutID).ToListAsync();
    Check(planReceipts.Count == 3 && planReceipts.Sum(row => row.TotalAmount) == 22320 &&
        (await db.Checkouts.FindAsync(plan.CheckoutID))!.Status == "Paid",
        "Completed plan contains down payment plus actual receipts, including interest, without a duplicate sale-total entry.");
    Check(!(await installments.RecordPaymentAsync(plan.InstallmentID, 1, "Cash")).Success,
        "A completed plan cannot receive another payment.");
    var installmentDetails = await installments.GetInstallmentDetailsAsync(plan.InstallmentID);
    Check(installmentDetails?.Installment.InstallmentPayments.Count == 2 &&
        installmentDetails.Installment.InstallmentPayments.All(payment => payment.PaymentID > 0),
        "Installment details loads every recorded payment for the payment table.");
    Check($"INS-{installmentDetails!.Installment.InstallmentID:D3}" == installmentDetails.Installment.FormattedInstallmentID,
        "Installment payment rows use the standard INS-001 display format.");
    Check(Success(sales.RefundCheckout(full.CheckoutID)) &&
        await db.CustomerPurchaseHistories.AnyAsync(row => row.CheckoutID == full.CheckoutID),
        "Refunding a checkout preserves its original payment history.");
    Check(!Success(sales.DeleteCheckout(full.CheckoutID)) && !Success(sales.DeleteCheckouts([full.CheckoutID, plan.CheckoutID])),
        "Single and bulk checkout deletion protect payment history.");
    var installmentController = new InstallmentController(installments, db, NullLogger<InstallmentController>.Instance);
    Check(!Success(installmentController.DeleteInstallments([plan.InstallmentID])),
        "Instalment deletion protects linked payment records.");

    var spy = new CaptureExport();
    var exporter = new ExcelExportController(db, spy, installments, NullLogger<ExcelExportController>.Instance);
    var selected = planReceipts[0];
    Check(await exporter.SalesHistory(new() { RecordIds = [selected.HistoryID] }) is FileContentResult &&
        spy.Columns.Count == 7 && spy.Rows.Count == 1 &&
        (string)spy.Rows[0][0]! == selected.FormattedHistoryID &&
        (string)spy.Rows[0][3]! == "Installment" &&
        (DateTime)spy.Rows[0][4]! == selected.PurchaseDate.Date &&
        spy.Columns.Any(column => column.Header == "Payment Type") &&
        spy.Columns.All(column => column.Header != "Email"),
        "Filtered export includes linked payment type, excludes email, and strips the date's time component.");
    db.ChangeTracker.Clear();
    var view = (ViewResult)await sales.SalesHistory(default);
    var visible = ((IEnumerable<CustomerPurchaseHistory>)view.Model!).ToList();
    Check(visible.Count == 8 && !db.ChangeTracker.Entries().Any() &&
        visible.SequenceEqual(visible.OrderByDescending(row => row.PurchaseDate).ThenByDescending(row => row.HistoryID)),
        "Sales History reads all receipts newest first without modifying records.");
    Console.WriteLine("All Sales History SQL Server integration checks passed.");
    keepPreview = args.Contains("--keep-preview");
    if (keepPreview) Console.WriteLine("PREVIEW_DATABASE=" + databaseName);
}
finally
{
    if (!keepPreview)
    {
        if (db.Database.GetDbConnection().Database != databaseName || !databaseName.StartsWith("RJTechSalesHistoryTest_"))
            throw new InvalidOperationException("Refusing to remove an unexpected database.");
        await db.Database.EnsureDeletedAsync();
    }
}

sealed class TestUrlHelper : IUrlHelper
{
    public ActionContext ActionContext { get; } = new();
    public string? Action(UrlActionContext context) => "/test/checkout";
    public string? Content(string? path) => path;
    public bool IsLocalUrl(string? url) => true;
    public string? Link(string? routeName, object? values) => null;
    public string? RouteUrl(UrlRouteContext context) => null;
}

sealed class CaptureExport : IExcelExportService
{
    public IReadOnlyList<ExcelExportColumn> Columns { get; private set; } = [];
    public List<IReadOnlyList<object?>> Rows { get; private set; } = [];
    public byte[] ExportToExcel(string title, IReadOnlyList<ExcelExportColumn> columns, IEnumerable<IReadOnlyList<object?>> rows)
    {
        Columns = columns;
        Rows = rows.ToList();
        return [1];
    }
}
