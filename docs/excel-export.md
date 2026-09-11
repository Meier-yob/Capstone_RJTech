# Excel exports

All five management pages download `.xlsx` workbooks using ClosedXML. Microsoft Excel is not required on the server. The existing database schema and CRUD actions are unchanged.

| Page | POST endpoint | Worksheet title | Filename module |
| --- | --- | --- | --- |
| Product Management | `/ExcelExport/Inventory` | INVENTORY | Inventory |
| Delivery Management | `/ExcelExport/Delivery` | DELIVERY | Delivery |
| Customer Management | `/ExcelExport/Customers` | CUSTOMERS | Customers |
| Sales Summary | `/ExcelExport/SalesSummary` | SALES SUMMARY | SalesSummary |
| Installment Management | `/ExcelExport/Installments` | INSTALLMENT | Installments |

Filenames include the Philippine date and time, including milliseconds. The workbook uses the existing `wwwroot/sources/logo-svg.png` when readable. Missing or invalid logos do not prevent exports.

## Filters and downloads

The shared browser helper sends `{ "recordIds": null }` when no filter is active. Otherwise, it sends the IDs of **all matching rows before pagination**. This reuses each page's existing search/category/status/progress logic. The current Sales Summary has search and status filters; it does not have month/year controls. Future filters should feed the same matching-row function and active-filter flag.

The server retrieves the current values of these records from the database; it does not accept spreadsheet cell values from the browser. Filtered exports reflect the selection from the loaded page with fresh database values. Refresh the page to include newly matching records created after it loaded. Sales and installments retain their existing exclusion of cancelled records. Delivery receipts use the table's existing Completed status.

Requests require the `RequestVerificationToken` antiforgery header. The `recordIds` property is required even when its value is null. Invalid selections and empty results return HTTP 400 with a readable JSON `message`; unexpected failures are logged and return HTTP 500. The browser displays these messages without navigating away. Responses disable caching.

## Reusing the design

Inject `IExcelExportService` and supply a title, ordered column metadata, and rows with matching values:

```csharp
IReadOnlyList<ExcelExportColumn> columns =
[
    new("Product", MinWidth: 30, MaxWidth: 40),
    new("Price", ExcelColumnType.Currency, 15, 18),
    new("Status", ExcelColumnType.Status, 15, 20)
];
IReadOnlyList<object?>[] rows =
[
    new object?[] { "Digital Timer", 275m, "Available" }
];
byte[] bytes = excel.ExportToExcel("INVENTORY", columns, rows);
return File(bytes, ExcelExportFile.ContentType,
    ExcelExportFile.CreateFileName("Inventory"));
```

Use strings for IDs and phone numbers, numeric values for money and quantities, and `DateTime`/`DateOnly` for dates. Percentage values are fractional (`0.05m` displays as `5%`). Nulls become blank cells. Text beginning with `=` remains literal text, not an Excel formula.

The shared service owns the merged title, row 5 header, AutoFilter, frozen rows 1–5, borders, alternating colors, typed formats, and status palette. Add future status colors there. Columns auto-size from the header and first 1,000 rows within the supplied bounds; subsequent long text wraps with adjusted row heights. Excel's worksheet row and cell-text limits are checked explicitly. ClosedXML builds workbooks in memory, so very large exports still depend on available server memory.

## Verification

Run `dotnet run --project tests/ExcelExportChecks` for workbook and isolated LocalDB integration checks, and `node --test tests/ExcelExportChecks/excel-export.test.cjs` for browser download/filter checks. See the test README for prerequisites and generated sample locations.
