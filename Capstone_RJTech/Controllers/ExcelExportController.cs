using Capstone_RJTech.Data;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Capstone_RJTech.Controllers;

public sealed class ExcelExportController : Controller
{
    private const string NoRecordsMessage = "No records available to export.";
    private readonly ApplicationDbContext _db;
    private readonly IExcelExportService _excel;
    private readonly InstallmentService _installments;
    private readonly ILogger<ExcelExportController> _logger;

    public ExcelExportController(
        ApplicationDbContext db,
        IExcelExportService excel,
        InstallmentService installments,
        ILogger<ExcelExportController> logger)
    {
        _db = db;
        _excel = excel;
        _installments = installments;
        _logger = logger;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Inventory(
        [FromBody] ExcelExportSelection? selection,
        CancellationToken cancellationToken = default)
        => ExportAsync(selection, "INVENTORY", "Inventory",
        [
            new("Brand", MinWidth: 15, MaxWidth: 22),
            new("Products", MinWidth: 30, MaxWidth: 40),
            new("Quantity", ExcelColumnType.Integer, 12, 15),
            new("Price", ExcelColumnType.Currency, 15, 18),
            new("Status", ExcelColumnType.Status, 15, 20)
        ], async (ids, token) =>
        {
            var query = _db.Products.AsNoTracking();
            if (ids != null)
                query = query.Where(product => ids.Contains(product.product_ID));

            // Only load fields required for the report and the existing stock status calculation.
            var products = await query
                .OrderBy(product => product.product_brand)
                .ThenBy(product => product.product_name)
                .ThenBy(product => product.product_ID)
                .Select(product => new Product
                {
                    product_brand = product.product_brand,
                    product_name = product.product_name,
                    product_quantity = product.product_quantity,
                    Product_price = product.Product_price,
                    product_status = product.product_status,
                    reorder_level = product.reorder_level
                })
                .ToListAsync(token);

            return ToRows(products, product =>
            [
                product.product_brand,
                product.product_name,
                product.product_quantity,
                product.Product_price,
                ProductController.EvaluateProductStatus(product)
            ]);
        }, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Delivery(
        [FromBody] ExcelExportSelection? selection,
        CancellationToken cancellationToken = default)
        => ExportAsync(selection, "DELIVERY", "Delivery",
        [
            new("Delivery ID", MinWidth: 12, MaxWidth: 15),
            new("Batch ID", MinWidth: 12, MaxWidth: 15),
            new("Received By", MinWidth: 25, MaxWidth: 30),
            new("Date Received", ExcelColumnType.Date, 18, 20),
            new("Products", ExcelColumnType.Integer, 12, 15),
            new("Total Units", ExcelColumnType.Integer, 12, 15),
            new("Status", ExcelColumnType.Status, 15, 20)
        ], async (ids, token) =>
        {
            var query = _db.Deliveries.AsNoTracking();
            if (ids != null)
                query = query.Where(delivery => ids.Contains(delivery.delivery_ID));

            var deliveries = await query
                .OrderByDescending(delivery => delivery.date_delivered)
                .ThenByDescending(delivery => delivery.delivery_ID)
                .Select(delivery => new
                {
                    delivery.delivery_ID,
                    delivery.received_by,
                    delivery.date_delivered,
                    Products = delivery.DeliveryDetails.Count,
                    TotalUnits = delivery.DeliveryDetails.Sum(detail => (long?)detail.product_quantity) ?? 0L
                })
                .ToListAsync(token);

            return ToRows(deliveries, delivery =>
            [
                $"DEL-{delivery.delivery_ID:D3}",
                $"BATCH-{delivery.delivery_ID:D3}",
                delivery.received_by,
                delivery.date_delivered,
                delivery.Products,
                delivery.TotalUnits,
                // Delivery Management records are received receipts and display Completed.
                "Completed"
            ]);
        }, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Customers(
        [FromBody] ExcelExportSelection? selection,
        CancellationToken cancellationToken = default)
        => ExportAsync(selection, "CUSTOMERS", "Customers",
        [
            new("Customer ID", MinWidth: 12, MaxWidth: 15),
            new("Customer Name", MinWidth: 25, MaxWidth: 30),
            new("Email", MinWidth: 30, MaxWidth: 35),
            new("Phone Number", MinWidth: 16, MaxWidth: 18),
            new("Address", MinWidth: 30, MaxWidth: 40)
        ], async (ids, token) =>
        {
            var query = _db.Customers.AsNoTracking();
            if (ids != null)
                query = query.Where(customer => ids.Contains(customer.customer_ID));

            var customers = await query
                .OrderBy(customer => customer.customer_FullName)
                .ThenBy(customer => customer.customer_ID)
                .Select(customer => new
                {
                    customer.customer_ID,
                    customer.customer_FullName,
                    customer.customer_Email,
                    customer.customer_Phone,
                    customer.customer_Address
                })
                .ToListAsync(token);

            return ToRows(customers, customer =>
            [
                $"CUS-{customer.customer_ID:D3}",
                customer.customer_FullName,
                customer.customer_Email,
                customer.customer_Phone,
                customer.customer_Address
            ]);
        }, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> SalesSummary(
        [FromBody] ExcelExportSelection? selection,
        CancellationToken cancellationToken = default)
        => ExportAsync(selection, "SALES SUMMARY", "SalesSummary",
        [
            new("Checkout ID", MinWidth: 12, MaxWidth: 15),
            new("Customer", MinWidth: 25, MaxWidth: 30),
            new("Payment Type", MinWidth: 18, MaxWidth: 22),
            new("Payment Method", MinWidth: 18, MaxWidth: 22),
            new("Date Purchased", ExcelColumnType.Date, 18, 20),
            new("Status", ExcelColumnType.Status, 15, 20),
            new("Total Amount", ExcelColumnType.Currency, 15, 18)
        ], async (ids, token) =>
        {
            // Match Sales Summary's applicable records, including refunded sales.
            var query = _db.Checkouts.AsNoTracking().Where(checkout => checkout.Status != "Cancelled");
            if (ids != null)
                query = query.Where(checkout => ids.Contains(checkout.CheckoutID));

            var checkouts = await query
                .OrderByDescending(checkout => checkout.DatePurchased)
                .ThenByDescending(checkout => checkout.CheckoutID)
                .Select(checkout => new
                {
                    checkout.CheckoutID,
                    CustomerName = checkout.Customer == null ? "Unknown customer" : checkout.Customer.customer_FullName,
                    checkout.PaymentType,
                    checkout.PaymentMethod,
                    checkout.DatePurchased,
                    checkout.Status,
                    checkout.TotalAmount
                })
                .ToListAsync(token);

            return ToRows(checkouts, checkout =>
            [
                $"CHK-{checkout.CheckoutID:D3}",
                checkout.CustomerName,
                checkout.PaymentType,
                checkout.PaymentMethod,
                checkout.DatePurchased,
                checkout.Status,
                checkout.TotalAmount
            ]);
        }, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> Installments(
        [FromBody] ExcelExportSelection? selection,
        CancellationToken cancellationToken = default)
        => ExportAsync(selection, "INSTALLMENT", "Installments",
        [
            new("Installment ID", MinWidth: 12, MaxWidth: 15),
            new("Checkout ID", MinWidth: 12, MaxWidth: 15),
            new("Customer", MinWidth: 25, MaxWidth: 30),
            new("Total Amount", ExcelColumnType.Currency, 15, 18),
            new("Down Payment", ExcelColumnType.Currency, 15, 18),
            new("Balance", ExcelColumnType.Currency, 15, 18),
            new("Monthly Payment", ExcelColumnType.Currency, 15, 18),
            new("Months Paid", ExcelColumnType.Integer, 12, 15),
            new("Months Remaining", ExcelColumnType.Integer, 12, 18),
            new("Next Due Date", ExcelColumnType.Date, 18, 20),
            new("Status", ExcelColumnType.Status, 15, 20)
        ], async (ids, token) =>
        {
            var query = _db.Installments.AsNoTracking().Where(installment => installment.Status != "Cancelled");
            if (ids != null)
                query = query.Where(installment => ids.Contains(installment.InstallmentID));

            var installments = await query
                .OrderByDescending(installment => installment.InstallmentID)
                .Select(installment => new
                {
                    installment.InstallmentID,
                    installment.CheckoutID,
                    CustomerName = installment.Checkout == null || installment.Checkout.Customer == null
                        ? "Unknown customer"
                        : installment.Checkout.Customer.customer_FullName,
                    installment.TotalAmount,
                    installment.DownPayment,
                    installment.Balance,
                    installment.MonthlyPayment,
                    installment.Months,
                    installment.StartDate,
                    installment.Status,
                    TotalPaid = installment.InstallmentPayments
                        .Where(payment => payment.Status == "Paid")
                        .Sum(payment => (decimal?)payment.PaymentAmount) ?? 0m
                })
                .ToListAsync(token);

            DateTime asOf = DateTime.Now;
            return ToRows(installments, item =>
            {
                // Use existing business calculations on a detached snapshot; export never saves changes.
                var snapshot = new Installment
                {
                    TotalAmount = item.TotalAmount,
                    Balance = item.Balance,
                    MonthlyPayment = item.MonthlyPayment,
                    Months = item.Months,
                    StartDate = item.StartDate,
                    Status = item.Status
                };
                snapshot.Status = _installments.DetermineInstallmentStatus(snapshot, item.TotalPaid, asOf);
                bool completed = snapshot.Status == "Completed";
                snapshot.MonthsPaid = completed
                    ? item.Months
                    : _installments.CalculateMonthsPaid(item.TotalPaid, item.MonthlyPayment, item.Months);
                int monthsRemaining = completed ? 0 : Math.Max(0, item.Months - snapshot.MonthsPaid);
                DateTime? nextDueDate = completed || snapshot.Status == "Cancelled"
                    ? null
                    : _installments.CalculateDisplayDate(snapshot);

                return
                [
                    $"INS-{item.InstallmentID:D3}",
                    $"CHK-{item.CheckoutID:D3}",
                    item.CustomerName,
                    item.TotalAmount,
                    item.DownPayment,
                    completed ? 0m : item.Balance,
                    item.MonthlyPayment,
                    snapshot.MonthsPaid,
                    monthsRemaining,
                    nextDueDate,
                    snapshot.Status
                ];
            });
        }, cancellationToken);

    [HttpPost]
    [ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> SalesHistory(
        [FromBody] ExcelExportSelection? selection,
        CancellationToken cancellationToken = default)
        => ExportAsync(selection, "SALES HISTORY", "SalesHistory",
        [
            new("History ID", MinWidth: 12, MaxWidth: 15),
            new("Customer", MinWidth: 25, MaxWidth: 30),
            new("Checkout ID", MinWidth: 12, MaxWidth: 15),
            new("Payment Type", MinWidth: 18, MaxWidth: 22),
            new("Purchase Date", ExcelColumnType.Date, 18, 20),
            new("Total Amount", ExcelColumnType.Currency, 15, 18),
            new("Payment Method", MinWidth: 18, MaxWidth: 22)
        ], async (ids, token) =>
        {
            var query = _db.CustomerPurchaseHistories.AsNoTracking();
            if (ids != null)
                query = query.Where(payment => ids.Contains(payment.HistoryID));

            var payments = await query
                .OrderByDescending(payment => payment.PurchaseDate)
                .ThenByDescending(payment => payment.HistoryID)
                .Select(payment => new
                {
                    payment.HistoryID,
                    CustomerName = payment.Customer == null ? "Unknown customer" : payment.Customer.customer_FullName,
                    payment.CheckoutID,
                    PaymentType = payment.Checkout == null ? "Unknown" : payment.Checkout.PaymentType,
                    payment.PurchaseDate,
                    payment.TotalAmount,
                    payment.PaymentMethod
                }).ToListAsync(token);

            return ToRows(payments, payment =>
            [
                $"PAY-{payment.HistoryID:D3}",
                payment.CustomerName,
                $"CHK-{payment.CheckoutID:D3}",
                payment.PaymentType,
                payment.PurchaseDate.Date,
                payment.TotalAmount,
                payment.PaymentMethod
            ]);
        }, cancellationToken);

    private async Task<IActionResult> ExportAsync(
        ExcelExportSelection? selection,
        string title,
        string fileModule,
        IReadOnlyList<ExcelExportColumn> columns,
        Func<int[]?, CancellationToken, Task<List<IReadOnlyList<object?>>>> retrieveRows,
        CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid || selection == null || selection.RecordIds?.Any(id => id <= 0) == true)
            return BadRequest(new { message = "Invalid export selection. Refresh the page and try again." });

        int[]? recordIds = selection.RecordIds?.Distinct().ToArray();
        if (recordIds is { Length: 0 })
            return BadRequest(new { message = NoRecordsMessage });

        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var rows = await retrieveRows(recordIds, cancellationToken);
            if (rows.Count == 0)
                return BadRequest(new { message = NoRecordsMessage });

            cancellationToken.ThrowIfCancellationRequested();
            byte[] excelBytes = _excel.ExportToExcel(title, columns, rows);
            cancellationToken.ThrowIfCancellationRequested();
            return File(excelBytes, ExcelExportFile.ContentType, ExcelExportFile.CreateFileName(fileModule));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return StatusCode(499);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to export {Module} records to Excel.", fileModule);
            return StatusCode(StatusCodes.Status500InternalServerError,
                new { message = "Unable to export the Excel file. Please try again." });
        }
    }

    private static List<IReadOnlyList<object?>> ToRows<T>(
        IEnumerable<T> records,
        Func<T, IReadOnlyList<object?>> project)
        => records.Select(project).ToList();
}
