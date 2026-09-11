using System.Text.Json;
using Capstone_RJTech.Controllers;
using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

internal static class EndpointChecks
{
    public static async Task RunAsync(IExcelExportService excel, Action<bool, string> check, string outputDirectory)
    {
        // Only this unique, disposable test database is created, seeded, or removed.
        var databaseName = "RJTechExcelExportTest_" + Guid.NewGuid().ToString("N");
        var writeGuard = new WriteGuard();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer($"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;TrustServerCertificate=True")
            .AddInterceptors(writeGuard)
            .Options;
        await using var db = new ApplicationDbContext(options);
        try
        {
            await db.Database.EnsureCreatedAsync();
            db.Products.RemoveRange(db.Products);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            var installments = new InstallmentService(db, NullLogger<InstallmentService>.Instance);
            var controller = new ExcelExportController(db, excel, installments, NullLogger<ExcelExportController>.Instance);
            var endpoints = new (string Name, Func<ExcelExportSelection?, CancellationToken, Task<IActionResult>> Action)[]
            {
                ("Inventory", controller.Inventory), ("Delivery", controller.Delivery), ("Customers", controller.Customers),
                ("SalesSummary", controller.SalesSummary), ("Installments", controller.Installments)
            };
            foreach (var endpoint in endpoints)
            {
                var result = await endpoint.Action(new() { RecordIds = null }, default);
                check(IsMessage(result, 400, "No records available to export."),
                    $"{endpoint.Name} endpoint explains an empty database without generating a workbook.");
            }

            var products = new[]
            {
                new Product { product_name = "Available monitor", product_brand = "ACER", category_ID = 1, product_quantity = 11, reorder_level = 5, product_status = "Low Stock", Product_price = 3850.25m },
                new Product { product_name = "Low stock processor", product_brand = "AMD", category_ID = 1, product_quantity = 5, reorder_level = 5, product_status = "Available", Product_price = 8995m },
                new Product { product_name = "Out of stock timer", product_brand = "ALLAN", category_ID = 1, product_quantity = 0, reorder_level = 5, product_status = "Available", Product_price = 275m },
                new Product { product_name = "Unavailable device", product_brand = "RJTECH", category_ID = 1, product_quantity = 99, reorder_level = 5, product_status = "Unavailable", Product_price = 1500m }
            };
            db.Products.AddRange(products);
            var customers = new[]
            {
                new Customer { customer_FullName = "Mark Santos", customer_Email = "mark.export.test@gmail.com", customer_Phone = "09123456789", customer_Address = "Makati" },
                new Customer { customer_FullName = "Maria Cruz", customer_Email = "maria.export.test@gmail.com", customer_Phone = "09987654321", customer_Address = "Manila" }
            };
            db.Customers.AddRange(customers);
            var deliveries = new[]
            {
                new Delivery
                {
                    received_by = "Test Admin", batch_ID = "internal-test-batch-a", date_delivered = new DateTime(2026, 8, 31),
                    DeliveryDetails = [new() { Product = products[0], product_quantity = 3 }, new() { Product = products[1], product_quantity = 5 }]
                },
                new Delivery { received_by = "Other Admin", batch_ID = "internal-test-batch-b", date_delivered = new DateTime(2026, 9, 1), is_archived = true }
            };
            db.Deliveries.AddRange(deliveries);
            Checkout Sale(string status, Customer customer, DateTime date, decimal amount = 12000m) => new()
            {
                Customer = customer, DatePurchased = date, TotalAmount = amount,
                PaymentMethod = "Cash", PaymentType = "Full Payment", Status = status
            };
            var augustSale = Sale("Paid", customers[0], new DateTime(2026, 8, 31, 23, 59, 59), 25999.50m);
            var overdueSale = Sale("Ongoing", customers[0], new DateTime(2026, 9, 1));
            var cancelledSale = Sale("Cancelled", customers[1], new DateTime(2026, 9, 2));
            var refundedSale = Sale("Refunded", customers[1], new DateTime(2026, 9, 3));
            var completedSale = Sale("Paid", customers[1], new DateTime(2026, 9, 4));
            var activeSale = Sale("Ongoing", customers[1], new DateTime(2026, 9, 5));
            db.Checkouts.AddRange(augustSale, overdueSale, cancelledSale, refundedSale, completedSale, activeSale);
            Installment Plan(Checkout sale, string status, decimal balance, DateTime startDate) => new()
            {
                Checkout = sale, Status = status, Months = 6, MonthsPaid = 0, MonthsRemaining = 6,
                TotalAmount = 6000m, DownPayment = 1500m, Balance = balance, MonthlyPayment = 1000m, StartDate = startDate
            };
            var overdue = Plan(overdueSale, "Active", 4500m, DateTime.Today.AddMonths(-4));
            overdue.InstallmentPayments =
            [
                new() { PaymentAmount = 1500m, PaymentMethod = "Cash", Status = "Paid" },
                new() { PaymentAmount = 3000m, PaymentMethod = "Cash", Status = "Cancelled" }
            ];
            var completed = Plan(completedSale, "Active", 0m, DateTime.Today.AddMonths(-8));
            var cancelled = Plan(cancelledSale, "Cancelled", 6000m, DateTime.Today.AddMonths(-1));
            var active = Plan(activeSale, "Active", 6000m, DateTime.Today);
            db.Installments.AddRange(overdue, completed, cancelled, active);
            await db.SaveChangesAsync();
            db.ChangeTracker.Clear();
            writeGuard.ReadOnly = true;

            async Task<XLWorkbook> Export(
                string module,
                Func<ExcelExportSelection?, CancellationToken, Task<IActionResult>> action,
                int[]? ids = null,
                bool saveSample = false)
            {
                var result = await action(new() { RecordIds = ids }, default);
                check(result is FileContentResult, $"{module} endpoint returns an Excel download from database records.");
                var file = (FileContentResult)result;
                check(file.ContentType == ExcelExportFile.ContentType && file.FileDownloadName.StartsWith($"RJTech_{module}_") &&
                      file.FileDownloadName.EndsWith(".xlsx"), $"{module} download sets the correct MIME type and filename.");
                if (saveSample)
                    File.WriteAllBytes(Path.Combine(outputDirectory, "Database_" + module + ".xlsx"), file.FileContents);
                return new XLWorkbook(new MemoryStream(file.FileContents));
            }

            using (var workbook = await Export("Inventory", controller.Inventory, saveSample: true))
            {
                var sheet = workbook.Worksheet(1);
                check(DataCount(sheet) == products.Length, "Unfiltered inventory contains every product.");
                var availableRow = DataRows(sheet).Single(row => row.Cell(2).GetString() == products[0].product_name);
                var lowRow = DataRows(sheet).Single(row => row.Cell(2).GetString() == products[1].product_name);
                var outRow = DataRows(sheet).Single(row => row.Cell(2).GetString() == products[2].product_name);
                check(availableRow.Cell(5).GetString() == "Available" && lowRow.Cell(5).GetString() == "Low Stock" &&
                      outRow.Cell(5).GetString() == "Out of Stock" && availableRow.Cell(4).GetValue<decimal>() == 3850.25m,
                    "Inventory uses current stock rules and actual decimal prices, including reorder and zero boundaries.");
            }
            using (var workbook = await Export("Inventory", controller.Inventory, [products[1].product_ID, products[1].product_ID]))
            {
                check(DataCount(workbook.Worksheet(1)) == 1 && workbook.Worksheet(1).Cell("B6").GetString() == products[1].product_name,
                    "Filtered inventory exports only matching products and deduplicates selected IDs.");
            }

            using (var workbook = await Export("Delivery", controller.Delivery, saveSample: true))
            {
                var sheet = workbook.Worksheet(1);
                var row = DataRows(sheet).Single(row => row.Cell(1).GetString() == deliveries[0].FormattedDeliveryID);
                var emptyRow = DataRows(sheet).Single(row => row.Cell(1).GetString() == deliveries[1].FormattedDeliveryID);
                check(DataCount(sheet) == 2 && row.Cell(2).GetString() == deliveries[0].FormattedBatchID &&
                      row.Cell(3).GetString() == "Test Admin" && row.Cell(5).GetDouble() == 2 && row.Cell(6).GetDouble() == 8 &&
                      row.Cell(7).GetString() == "Completed" && emptyRow.Cell(5).GetDouble() == 0 && emptyRow.Cell(6).GetDouble() == 0,
                    "Delivery exports displayed IDs, receipt fields, product count, total units, and zero-detail receipts.");
            }
            using (var workbook = await Export("Delivery", controller.Delivery, [deliveries[0].delivery_ID]))
                check(DataCount(workbook.Worksheet(1)) == 1 && workbook.Worksheet(1).Cell("A6").GetString() == deliveries[0].FormattedDeliveryID,
                    "Delivery selection excludes unrelated receipts.");

            using (var workbook = await Export("Customers", controller.Customers, saveSample: true))
                check(DataCount(workbook.Worksheet(1)) == 2, "Unfiltered customers include all database customers.");
            using (var workbook = await Export("Customers", controller.Customers, [customers[0].customer_ID]))
            {
                var sheet = workbook.Worksheet(1);
                check(DataCount(sheet) == 1 && sheet.Cell("A6").GetString() == $"CUS-{customers[0].customer_ID:D3}" &&
                      sheet.Cell("B6").GetString() == "Mark Santos" && sheet.Cell("C6").GetString() == customers[0].customer_Email &&
                      sheet.Cell("D6").GetString() == "09123456789" && sheet.Cell("E6").GetString() == "Makati",
                    "Customer selection preserves public details, formatted IDs, and leading-zero phone numbers.");
            }

            using (var workbook = await Export("SalesSummary", controller.SalesSummary, saveSample: true))
            {
                var sheet = workbook.Worksheet(1);
                check(DataCount(sheet) == 5 && DataRows(sheet).Any(row => row.Cell(1).GetString() == refundedSale.FormattedCheckoutID) &&
                      DataRows(sheet).All(row => row.Cell(1).GetString() != cancelledSale.FormattedCheckoutID),
                    "Sales Summary matches applicable records: refunds remain included and cancelled sales are excluded.");
            }
            var augustIds = await db.Checkouts.AsNoTracking()
                .Where(sale => sale.DatePurchased >= new DateTime(2026, 8, 1) && sale.DatePurchased < new DateTime(2026, 9, 1))
                .Select(sale => sale.CheckoutID).ToArrayAsync();
            using (var workbook = await Export("SalesSummary", controller.SalesSummary, augustIds))
            {
                var sheet = workbook.Worksheet(1);
                check(DataCount(sheet) == 1 && sheet.Cell("A6").GetString() == augustSale.FormattedCheckoutID &&
                      sheet.Cell("B6").GetString() == "Mark Santos" && sheet.Cell("E6").GetDateTime() == augustSale.DatePurchased &&
                      sheet.Cell("G6").GetValue<decimal>() == 25999.50m,
                    "An August selection retains the final August sale and excludes September while preserving amount and customer.");
            }

            using (var workbook = await Export("Installments", controller.Installments, saveSample: true))
            {
                var sheet = workbook.Worksheet(1);
                var dueRow = DataRows(sheet).Single(row => row.Cell(1).GetString() == overdue.FormattedInstallmentID);
                var doneRow = DataRows(sheet).Single(row => row.Cell(1).GetString() == completed.FormattedInstallmentID);
                var activeRow = DataRows(sheet).Single(row => row.Cell(1).GetString() == active.FormattedInstallmentID);
                check(DataCount(sheet) == 3 && DataRows(sheet).All(row => row.Cell(1).GetString() != cancelled.FormattedInstallmentID),
                    "Unfiltered installments include every applicable plan and exclude cancelled plans.");
                check(dueRow.Cell(2).GetString() == overdueSale.FormattedCheckoutID && dueRow.Cell(6).GetValue<decimal>() == 4500m &&
                      dueRow.Cell(8).GetDouble() == 1 && dueRow.Cell(9).GetDouble() == 5 &&
                      dueRow.Cell(10).GetDateTime() == overdue.StartDate.AddMonths(2) && dueRow.Cell(11).GetString() == "Overdue",
                    "Installment projections use paid payments, actual balance, current overdue rules, and the next unpaid due date.");
                check(doneRow.Cell(6).GetValue<decimal>() == 0m && doneRow.Cell(8).GetDouble() == 6 && doneRow.Cell(9).GetDouble() == 0 &&
                      doneRow.Cell(10).IsEmpty() && doneRow.Cell(11).GetString() == "Completed" && activeRow.Cell(11).GetString() == "Active",
                    "Completed installment exports normalize paid months and blank next-due dates; current plans stay Active.");
            }
            using (var workbook = await Export("Installments", controller.Installments, [overdue.InstallmentID]))
                check(DataCount(workbook.Worksheet(1)) == 1 && workbook.Worksheet(1).Cell("A6").GetString() == overdue.FormattedInstallmentID,
                    "Installment selection exports only the matching plan.");

            foreach (var endpoint in endpoints)
            {
                check(IsMessage(await endpoint.Action(new() { RecordIds = [] }, default), 400, "No records available to export.") &&
                      IsMessage(await endpoint.Action(new() { RecordIds = [int.MaxValue] }, default), 400, "No records available to export."),
                    $"{endpoint.Name} empty and no-match selections explain that there are no records.");
            }
            check(IsMessage(await controller.Inventory(null), 400, "Invalid export selection.") &&
                  IsMessage(await controller.Inventory(new() { RecordIds = [-1] }), 400, "Invalid export selection."),
                "Invalid or missing selections fail safely instead of silently exporting every record.");
            var cancelledResult = await controller.Customers(new() { RecordIds = null }, new CancellationToken(true));
            check(cancelledResult is StatusCodeResult { StatusCode: 499 }, "Cancelled downloads exit without generating a workbook.");
            var failingController = new ExcelExportController(db, new FailingExporter(), installments, NullLogger<ExcelExportController>.Instance);
            check(IsMessage(await failingController.Customers(new() { RecordIds = null }), 500, "Unable to export the Excel file. Please try again."),
                "Excel generation failures return a safe user-facing error instead of crashing or leaking exception details.");

            check(!db.ChangeTracker.Entries().Any() && writeGuard.Attempts == 0 &&
                  await db.Products.AsNoTracking().Where(product => product.product_ID == products[0].product_ID)
                      .Select(product => product.product_status).SingleAsync() == "Low Stock" &&
                  await db.Installments.AsNoTracking().Where(plan => plan.InstallmentID == overdue.InstallmentID)
                      .Select(plan => plan.Status).SingleAsync() == "Active" &&
                  await db.Installments.AsNoTracking().Where(plan => plan.InstallmentID == completed.InstallmentID)
                      .Select(plan => plan.MonthsPaid).SingleAsync() == 0,
                "All export actions leave the change tracker empty and never save stock or installment status changes.");
            Console.WriteLine("All isolated SQL Server export endpoint checks passed.");
        }
        finally
        {
            if (db.Database.GetDbConnection().Database != databaseName || !databaseName.StartsWith("RJTechExcelExportTest_"))
                throw new InvalidOperationException("Refusing to clean up an unexpected database.");
            await db.Database.EnsureDeletedAsync();
        }
    }

    private static bool IsMessage(IActionResult result, int statusCode, string text)
        => result is ObjectResult response && response.StatusCode == statusCode &&
           JsonSerializer.Serialize(response.Value).Contains(text, StringComparison.Ordinal);

    private static int DataCount(IXLWorksheet sheet) => sheet.LastRowUsed()!.RowNumber() - 5;
    private static IEnumerable<IXLRow> DataRows(IXLWorksheet sheet) => sheet.Rows(6, sheet.LastRowUsed()!.RowNumber());

    private sealed class FailingExporter : IExcelExportService
    {
        public byte[] ExportToExcel(string title, IReadOnlyList<ExcelExportColumn> columns, IEnumerable<IReadOnlyList<object?>> rows)
            => throw new IOException("Internal test failure; must never be sent to the admin.");
    }

    private sealed class WriteGuard : SaveChangesInterceptor
    {
        public bool ReadOnly { get; set; }
        public int Attempts { get; private set; }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            RejectWrites();
            return result;
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            RejectWrites();
            return ValueTask.FromResult(result);
        }

        private void RejectWrites()
        {
            if (!ReadOnly) return;
            Attempts++;
            throw new InvalidOperationException("Exports must not save database changes.");
        }
    }
}
