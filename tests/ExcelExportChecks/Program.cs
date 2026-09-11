using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Xml.Linq;
using Capstone_RJTech.Models;
using Capstone_RJTech.Services;
using ClosedXML.Excel;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

// Workbook checks are database-free; endpoint checks use a unique disposable LocalDB database.
// Neither path touches application records.
// Default output is in the ignored build folder; pass an output directory to keep samples elsewhere.
var projectRoot = FindProjectRoot();
var runDatabaseChecks = !args.Contains("--workbook-only");
var outputArgument = args.FirstOrDefault(argument => argument != "--workbook-only");
var outputDirectory = outputArgument == null
    ? Path.Combine(AppContext.BaseDirectory, "samples")
    : Path.GetFullPath(outputArgument);
Directory.CreateDirectory(outputDirectory);
var environment = new CheckEnvironment(Path.Combine(projectRoot, "wwwroot"));
using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
var service = new ExcelExportService(environment, loggerFactory.CreateLogger<ExcelExportService>());
var checks = 0;

void Check(bool condition, string description)
{
    if (!condition) throw new InvalidOperationException(description);
    checks++;
    Console.WriteLine("PASS " + description);
}

byte[] ExportSample(string title, IReadOnlyList<ExcelExportColumn> columns, IEnumerable<IReadOnlyList<object?>> rows)
{
    var bytes = service.ExportToExcel(title, columns, rows);
    Check(bytes.Length > 0, $"{title} generates a nonempty .xlsx file.");
    File.WriteAllBytes(Path.Combine(outputDirectory, title.Replace(' ', '_') + ".xlsx"), bytes);
    return bytes;
}

IReadOnlyList<ExcelExportColumn> inventoryColumns =
[
    new("Brand", MinWidth: 12, MaxWidth: 18),
    new("Products", MinWidth: 30, MaxWidth: 40),
    new("Quantity", ExcelColumnType.Integer, 12, 15),
    new("Price", ExcelColumnType.Currency, 15, 18),
    new("Status", ExcelColumnType.Status, 15, 20)
];
IReadOnlyList<IReadOnlyList<object?>> inventoryRows =
[
    new object?[] { "ACER", "K202 Q 20\" LED MONITOR", 0, 3850m, "Out of Stock" },
    new object?[] { "AMD", "Ryzen 5 5600GT Processor", 3, 8995m, "Low Stock" },
    new object?[] { "ALLAN", "Digital Timer", 10, 275m, "Available" },
    new object?[] { "RJTECH", "A very long product description " + new string('W', 150), 25, 25999.25m, "Unavailable" }
];
var inventoryBytes = ExportSample("INVENTORY", inventoryColumns, inventoryRows);
using (var workbook = Open(inventoryBytes))
{
    var sheet = workbook.Worksheet(1);
    Check(workbook.Worksheets.Count == 1 && sheet.Cell("A1").GetString() == "INVENTORY",
        "Workbook opens with a single worksheet and the requested title.");
    Check(sheet.MergedRanges.Any(range => range.RangeAddress.ToStringRelative() == "A1:E3"),
        "Title spans all five columns and three rows.");
    Check(sheet.Cell("A1").Style.Font.Bold && sheet.Cell("A1").Style.Font.FontSize is >= 20 and <= 24 &&
          sheet.Cell("A1").Style.Alignment.Horizontal == XLAlignmentHorizontalValues.Center &&
          sheet.Cell("A1").Style.Alignment.Vertical == XLAlignmentVerticalValues.Center,
        "Title is large, bold, and centered horizontally and vertically.");
    Check(sheet.Row(5).Cells(1, 5).Select(cell => cell.GetString()).SequenceEqual(inventoryColumns.Select(column => column.Header)),
        "Header row preserves the requested column order and labels.");
    Check(sheet.Row(5).Cells(1, 5).All(cell => cell.Style.Font.Bold &&
          cell.Style.Font.FontColor.Color.ToArgb() == System.Drawing.Color.White.ToArgb() &&
          cell.Style.Border.BottomBorder == XLBorderStyleValues.Thin),
        "All headers have bold white text and thin borders.");
    Check(sheet.Cell("B6").Style.Fill.BackgroundColor != sheet.Cell("B7").Style.Fill.BackgroundColor &&
          sheet.Cell("B6").Style.Fill.BackgroundColor == sheet.Cell("B8").Style.Fill.BackgroundColor &&
          sheet.Cell("B6").Style.Border.BottomBorder == XLBorderStyleValues.Thin,
        "Data rows alternate background colors and have thin borders.");
    Check(sheet.Cell("C6").DataType == XLDataType.Number && sheet.Cell("C6").GetDouble() == 0 &&
          sheet.Cell("C6").Style.NumberFormat.Format == "0",
        "Zero quantities remain numeric whole numbers.");
    Check(sheet.Cell("D9").DataType == XLDataType.Number && sheet.Cell("D9").GetValue<decimal>() == 25999.25m &&
          sheet.Cell("D9").Style.NumberFormat.Format.Contains('₱') &&
          sheet.Cell("D9").Style.NumberFormat.Format.Contains("#,##0.00"),
        "Peso prices remain numeric with thousands separators and two decimal places.");
    Check(Enumerable.Range(1, inventoryColumns.Count).All(index =>
          sheet.Column(index).Width >= inventoryColumns[index - 1].MinWidth &&
          sheet.Column(index).Width <= inventoryColumns[index - 1].MaxWidth + 0.01) &&
          sheet.Cell("B9").Style.Alignment.WrapText,
        "Columns respect configured width bounds and long product names wrap.");
    Check(sheet.Row(9).Height >= 130 && sheet.Row(9).Height > sheet.Row(8).Height,
        "Wrapped long product names have enough row height for wide-character text.");
    Check(Enumerable.Range(6, 4).Select(row => sheet.Cell(row, 5).Style.Fill.BackgroundColor).Distinct().Count() == 4,
        "Available, low stock, out of stock, and unavailable receive distinct status fills.");
    Check(sheet.Pictures.Any(), "The existing RJTech logo is embedded in the workbook.");
}
using (var archive = new ZipArchive(new MemoryStream(inventoryBytes)))
{
    XNamespace spreadsheet = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
    using var xml = archive.GetEntry("xl/worksheets/sheet1.xml")!.Open();
    var sheet = XDocument.Load(xml);
    Check(sheet.Descendants(spreadsheet + "pane").Any(pane =>
          (string?)pane.Attribute("state") is "frozen" or "frozenSplit" && (string?)pane.Attribute("ySplit") == "5"),
        "Saved Excel XML freezes the title and table header through row five.");
    Check(sheet.Descendants(spreadsheet + "autoFilter").Any(filter => (string?)filter.Attribute("ref") == "A5:E9"),
        "Saved Excel AutoFilter includes the header and every inventory record.");
}

IReadOnlyList<ExcelExportColumn> customerColumns =
[
    new("Customer ID", MinWidth: 12, MaxWidth: 15),
    new("Customer Name", MinWidth: 25, MaxWidth: 30),
    new("Email", MinWidth: 30, MaxWidth: 35),
    new("Phone Number", MinWidth: 15, MaxWidth: 20)
];
var formulaText = "=HYPERLINK(\"https://example.invalid\",\"Customer\")";
var customerBytes = ExportSample("CUSTOMERS", customerColumns,
[
    new object?[] { "CUS-001", "Mark Santos", "mark@email.com", "09123456789" },
    new object?[] { "CUS-002", formulaText, null, "+639123456789" },
    new object?[] { "CUS-003", "@SUM(1,2)", "-2+3", "001234" }
]);
using (var workbook = Open(customerBytes))
{
    var sheet = workbook.Worksheet(1);
    Check(sheet.Cell("D6").DataType == XLDataType.Text && sheet.Cell("D6").GetString() == "09123456789" &&
          sheet.Cell("D8").GetString() == "001234",
        "Phone numbers preserve leading zeros as text.");
    Check(sheet.Cell("B7").GetString() == formulaText && sheet.Cell("D7").GetString() == "+639123456789" &&
          sheet.Cell("B8").GetString() == "@SUM(1,2)" && sheet.Cell("C8").GetString() == "-2+3" &&
          sheet.CellsUsed().All(cell => !cell.HasFormula),
        "Formula-looking customer values remain literal text and create no Excel formulas.");
    Check(sheet.Cell("C7").IsEmpty(), "Null customer values export as blank cells.");
}

IReadOnlyList<ExcelExportColumn> salesColumns =
[
    new("Checkout ID", MinWidth: 12, MaxWidth: 15),
    new("Customer", MinWidth: 25, MaxWidth: 30),
    new("Payment Method", MinWidth: 18, MaxWidth: 22),
    new("Date Purchased", ExcelColumnType.Date, 18, 20),
    new("Status", ExcelColumnType.Status, 15, 20),
    new("Total Amount", ExcelColumnType.Currency, 15, 18)
];
var purchaseDate = new DateTime(2026, 9, 6);
var salesBytes = ExportSample("SALES SUMMARY", salesColumns,
[
    new object?[] { "CHK-001", "Mark Santos", "Cash", purchaseDate, "Paid", 25500m },
    new object?[] { "CHK-002", "Maria Cruz", "Cash", purchaseDate, "Ongoing", 1500m },
    new object?[] { "CHK-003", "Juan Reyes", "GCash", purchaseDate, "Cancelled", 120500m },
    new object?[] { "CHK-004", "Liza Diaz", "Cash", purchaseDate, "Refunded", 25999m }
]);
using (var workbook = Open(salesBytes))
{
    var sheet = workbook.Worksheet(1);
    Check(sheet.Cell("A1").GetString() == "SALES SUMMARY" && sheet.Cell("A6").GetString() == "CHK-001",
        "Sales exports retain the module title and formatted checkout ID.");
    Check(sheet.Cell("D6").DataType == XLDataType.DateTime && sheet.Cell("D6").GetDateTime() == purchaseDate &&
          sheet.Cell("D6").Style.NumberFormat.Format.Equals("MMM dd, yyyy", StringComparison.OrdinalIgnoreCase),
        "Purchase dates roundtrip as Excel dates using the requested display format.");
    Check(sheet.Cell("E6").Style.Fill.BackgroundColor != sheet.Cell("E7").Style.Fill.BackgroundColor &&
          sheet.Cell("E6").Style.Fill.BackgroundColor != sheet.Cell("E8").Style.Fill.BackgroundColor &&
          sheet.Cell("E6").Style.Fill.BackgroundColor != sheet.Cell("E9").Style.Fill.BackgroundColor,
        "Paid sales are visually distinguished from ongoing, cancelled, and refunded sales.");
}

ExportSample("DELIVERY",
[
    new("Delivery ID"), new("Batch ID"), new("Date Received", ExcelColumnType.Date, 18, 20),
    new("Received By", MinWidth: 20, MaxWidth: 30), new("Items", ExcelColumnType.Integer),
    new("Status", ExcelColumnType.Status, 15, 20)
],
[
    new object?[] { "DEL-001", "BATCH-001", purchaseDate, "Admin", 8, "Completed" },
    new object?[] { "DEL-002", "BATCH-002", null, "Admin", 2, "Pending" },
    new object?[] { "DEL-003", "BATCH-003", null, "Admin", 1, "Cancelled" }
]);

IReadOnlyList<ExcelExportColumn> installmentColumns =
[
    new("Installment ID", MinWidth: 12, MaxWidth: 15), new("Checkout ID", MinWidth: 12, MaxWidth: 15),
    new("Customer", MinWidth: 25, MaxWidth: 30), new("Total Amount", ExcelColumnType.Currency, 15, 18),
    new("Down Payment", ExcelColumnType.Currency, 15, 18), new("Balance", ExcelColumnType.Currency, 15, 18),
    new("Monthly Payment", ExcelColumnType.Currency, 15, 18), new("Months Paid", ExcelColumnType.Integer),
    new("Months Remaining", ExcelColumnType.Integer), new("Next Due Date", ExcelColumnType.Date, 18, 20),
    new("Status", ExcelColumnType.Status, 15, 20)
];
var installmentBytes = ExportSample("INSTALLMENT", installmentColumns,
[
    new object?[] { "INS-004", "CHK-018", "Mark Santos", 39000m, 7800m, 19500m, 3250m, 3, 9, new DateTime(2026, 8, 25), "Overdue" },
    new object?[] { "INS-005", "CHK-019", "Maria Cruz", 20000m, 4000m, 0m, 2000m, 8, 0, null, "Completed" },
    new object?[] { "INS-006", "CHK-020", "Juan Reyes", 20000m, 4000m, 10000m, 2000m, 3, 5, new DateTime(2026, 10, 1), "Active" }
]);
using (var workbook = Open(installmentBytes))
{
    var sheet = workbook.Worksheet(1);
    Check(sheet.Cell("A6").GetString() == "INS-004" && sheet.Cell("B6").GetString() == "CHK-018" &&
          sheet.Cell("J7").IsEmpty() && sheet.Cell("F7").GetValue<decimal>() == 0m &&
          sheet.Cell("K6").Style.Fill.BackgroundColor != sheet.Cell("K7").Style.Fill.BackgroundColor &&
          sheet.Cell("K6").Style.Fill.BackgroundColor != sheet.Cell("K8").Style.Fill.BackgroundColor,
        "Installments retain formatted IDs, zero balances, nullable due dates, and distinct overdue status.");
}

var typeBytes = service.ExportToExcel("TYPES", [new("Interest Rate", ExcelColumnType.Percentage), new("Null Date", ExcelColumnType.Date)],
    [new object?[] { 0.15m, null }]);
using (var workbook = Open(typeBytes))
{
    var sheet = workbook.Worksheet(1);
    Check(sheet.Cell("A6").DataType == XLDataType.Number && sheet.Cell("A6").GetValue<decimal>() == 0.15m &&
          sheet.Cell("A6").Style.NumberFormat.Format.Contains('%') && sheet.Cell("B6").IsEmpty(),
        "Percentage columns preserve fractional numeric values and null dates stay blank.");
}

var missingLogoService = new ExcelExportService(new CheckEnvironment(Path.Combine(outputDirectory, "missing-web-root")),
    NullLogger<ExcelExportService>.Instance);
using (var workbook = Open(missingLogoService.ExportToExcel("CUSTOMERS", customerColumns,
    [new object?[] { "CUS-001", "Test Customer", null, null }])))
{
    Check(workbook.Worksheet(1).Cell("B6").GetString() == "Test Customer" && !workbook.Worksheet(1).Pictures.Any(),
        "Missing optional logo does not prevent a valid export.");
}

var corruptWebRoot = Path.Combine(outputDirectory, "corrupt-web-root");
Directory.CreateDirectory(Path.Combine(corruptWebRoot, "sources"));
File.WriteAllText(Path.Combine(corruptWebRoot, "sources", "logo-svg.png"), "This fixture is intentionally not a PNG.");
var corruptLogoService = new ExcelExportService(new CheckEnvironment(corruptWebRoot), NullLogger<ExcelExportService>.Instance);
using (var workbook = Open(corruptLogoService.ExportToExcel("CUSTOMERS", customerColumns,
    [new object?[] { "CUS-001", "Test Customer", null, null }])))
{
    Check(workbook.Worksheet(1).Cell("B6").GetString() == "Test Customer" && !workbook.Worksheet(1).Pictures.Any(),
        "An unreadable logo is safely omitted without losing exported records.");
}

var emptyRejected = false;
try
{
    service.ExportToExcel("INVENTORY", inventoryColumns, Array.Empty<IReadOnlyList<object?>>());
}
catch (InvalidOperationException exception)
{
    emptyRejected = exception.Message.Contains("No records", StringComparison.OrdinalIgnoreCase);
}
Check(emptyRejected, "Empty data produces a controlled no-records explanation instead of an empty workbook.");

var oversizedRejected = false;
try
{
    service.ExportToExcel("INVENTORY", inventoryColumns,
        Enumerable.Repeat<IReadOnlyList<object?>>(Array.Empty<object?>(), 1_048_576));
}
catch (InvalidOperationException exception)
{
    oversizedRejected = exception.Message.Contains("Too many records", StringComparison.OrdinalIgnoreCase);
}
Check(oversizedRejected, "Known record counts beyond Excel's worksheet limit fail before reading or allocating all rows.");

foreach (var module in new[] { "Inventory", "Delivery", "Customers", "SalesSummary", "Installments" })
{
    var fileName = ExcelExportFile.CreateFileName(module);
    Check(Regex.IsMatch(fileName, $"^RJTech_{module}_\\d{{4}}-\\d{{2}}-\\d{{2}}_\\d{{4,6}}(?:_\\d{{3}})?\\.xlsx$"),
        $"{module} filename includes the RJTech prefix, date, time, and .xlsx extension.");
}
Check(ExcelExportFile.ContentType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    "Downloads declare the correct Excel workbook MIME type.");

var missingSelectionRejected = false;
try
{
    JsonSerializer.Deserialize<ExcelExportSelection>("{}", new JsonSerializerOptions(JsonSerializerDefaults.Web));
}
catch (JsonException)
{
    missingSelectionRejected = true;
}
Check(missingSelectionRejected &&
      JsonSerializer.Deserialize<ExcelExportSelection>("{\"recordIds\":null}", new JsonSerializerOptions(JsonSerializerDefaults.Web)) is { RecordIds: null },
    "Missing JSON selection fields are rejected; an explicit null requests all applicable records.");

const int largeRowCount = 10_000;
var stopwatch = Stopwatch.StartNew();
var largeRows = new SinglePassRows(LargeRows(largeRowCount));
var largeBytes = service.ExportToExcel("INVENTORY", inventoryColumns, largeRows);
stopwatch.Stop();
Check(largeRows.Enumerations == 1, "Streamed source records are enumerated exactly once.");
using (var workbook = Open(largeBytes))
{
    var sheet = workbook.Worksheet(1);
    Check(sheet.LastRowUsed()!.RowNumber() == largeRowCount + 5 &&
          sheet.Cell(largeRowCount + 5, 2).GetString() == $"Product {largeRowCount}" &&
          sheet.Cell(largeRowCount + 5, 3).GetDouble() == largeRowCount,
        "A streamed 10,000-record export roundtrips without dropping or truncating rows.");
}
Console.WriteLine($"Large export: {largeRowCount:N0} rows, {largeBytes.Length:N0} bytes, {stopwatch.Elapsed.TotalSeconds:F2} seconds.");
if (runDatabaseChecks)
    await EndpointChecks.RunAsync(service, Check, outputDirectory);
Console.WriteLine($"All {checks} Excel export checks passed. Samples: {outputDirectory}");

static XLWorkbook Open(byte[] bytes) => new(new MemoryStream(bytes));

static IEnumerable<IReadOnlyList<object?>> LargeRows(int count)
{
    for (var index = 1; index <= count; index++)
        yield return new object?[] { "RJTECH", $"Product {index}", index, index + 0.25m, "Available" };
}

static string FindProjectRoot()
{
    var directory = new DirectoryInfo(AppContext.BaseDirectory);
    while (directory is not null)
    {
        var candidate = Path.Combine(directory.FullName, "Capstone_RJTech", "Capstone_RJTech.csproj");
        if (File.Exists(candidate)) return Path.GetDirectoryName(candidate)!;
        directory = directory.Parent;
    }
    throw new DirectoryNotFoundException("Could not find the Capstone_RJTech project from this executable.");
}

sealed class CheckEnvironment(string webRootPath) : IWebHostEnvironment
{
    public string ApplicationName { get; set; } = "ExcelExportChecks";
    public string EnvironmentName { get; set; } = "Development";
    public string WebRootPath { get; set; } = webRootPath;
    public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    public string ContentRootPath { get; set; } = Path.GetDirectoryName(webRootPath)!;
    public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
}

sealed class SinglePassRows(IEnumerable<IReadOnlyList<object?>> rows) : IEnumerable<IReadOnlyList<object?>>
{
    public int Enumerations { get; private set; }

    public IEnumerator<IReadOnlyList<object?>> GetEnumerator()
    {
        if (++Enumerations != 1)
            throw new InvalidOperationException("The source records must not be enumerated a second time.");
        return rows.GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}
