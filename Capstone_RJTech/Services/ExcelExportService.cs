using ClosedXML.Excel;
using ClosedXML.Excel.Drawings;
using ClosedXML.Graphics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Capstone_RJTech.Services;

/// <summary>One RJTech workbook design shared by all management exports.</summary>
public sealed class ExcelExportService(
    IWebHostEnvironment environment,
    ILogger<ExcelExportService> logger) : IExcelExportService
{
    private const int HeaderRow = 5;
    private const int MaxDataRows = 1_048_576 - HeaderRow;
    private const int WidthSampleRows = 1_000;
    private static readonly XLColor Navy = XLColor.FromHtml("#17365D");
    private static readonly XLColor Blue = XLColor.FromHtml("#245A81");
    private static readonly XLColor Stripe = XLColor.FromHtml("#EAF3FA");
    private static readonly XLColor Border = XLColor.FromHtml("#C5D8E8");

    public byte[] ExportToExcel(
        string title,
        IReadOnlyList<ExcelExportColumn> columns,
        IEnumerable<IReadOnlyList<object?>> rows)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(title);
        ArgumentNullException.ThrowIfNull(columns);
        ArgumentNullException.ThrowIfNull(rows);
        if (columns.Count is < 1 or > 16_384)
            throw new ArgumentException("An Excel export must have between 1 and 16,384 columns.", nameof(columns));
        if (columns.Any(column => string.IsNullOrWhiteSpace(column.Header) ||
            !double.IsFinite(column.MinWidth) || !double.IsFinite(column.MaxWidth) ||
            column.MinWidth <= 0 || column.MaxWidth < column.MinWidth || column.MaxWidth > 255))
            throw new ArgumentException("Every export column must have a heading and valid width bounds.", nameof(columns));
        if (rows.TryGetNonEnumeratedCount(out var knownCount))
        {
            if (knownCount == 0)
                throw new InvalidOperationException("No records available to export.");
            if (knownCount > MaxDataRows)
                throw new InvalidOperationException("Too many records for one Excel worksheet. Narrow the filters and try again.");
        }

        using var workbook = new XLWorkbook();
        workbook.Properties.Author = "RJTech";
        workbook.Properties.Title = title;
        workbook.Style.Font.FontName = "Calibri";
        workbook.Style.Font.FontSize = 11;

        var worksheet = workbook.Worksheets.Add(SheetName(title));
        worksheet.ShowGridLines = false;
        var titleRange = worksheet.Range(1, 1, 3, columns.Count).Merge();
        titleRange.FirstCell().Value = title;
        titleRange.Style.Font.FontSize = 24;
        titleRange.Style.Font.Bold = true;
        titleRange.Style.Font.FontColor = Navy;
        titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        worksheet.Rows(1, 3).Height = 32;
        worksheet.Row(4).Height = 14;

        for (int index = 0; index < columns.Count; index++)
            worksheet.Cell(HeaderRow, index + 1).Value = columns[index].Header;

        // Write the source once. InsertData may probe lazy row sources more than once.
        // Range formatting below remains batched, and no second data buffer is needed.
        int count = 0;
        foreach (var row in rows)
        {
            if (++count > MaxDataRows)
                throw new InvalidOperationException("Too many records for one Excel worksheet. Narrow the filters and try again.");
            if (row == null || row.Count != columns.Count)
                throw new ArgumentException("Each export row must match the configured columns.", nameof(rows));
            for (int index = 0; index < row.Count; index++)
            {
                object? value = row[index];
                if (value is string text && text.Length > 32_767)
                    throw new InvalidOperationException("A record contains more text than an Excel cell can hold.");
                if (value is DateOnly date) value = date.ToDateTime(TimeOnly.MinValue);
                if (value is DBNull) value = null;
                // ClosedXML writes strings as literal values (including leading '='), never formulas.
                worksheet.Cell(HeaderRow + count, index + 1).Value = XLCellValue.FromObject(value, CultureInfo.InvariantCulture);
            }
        }
        if (count == 0)
            throw new InvalidOperationException("No records available to export.");

        int lastRow = HeaderRow + count;
        var table = worksheet.Range(HeaderRow, 1, lastRow, columns.Count);
        table.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        table.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        table.Style.Border.OutsideBorderColor = Border;
        table.Style.Border.InsideBorderColor = Border;
        table.SetAutoFilter();

        var header = worksheet.Range(HeaderRow, 1, HeaderRow, columns.Count);
        header.Style.Fill.BackgroundColor = Blue;
        header.Style.Font.FontColor = XLColor.White;
        header.Style.Font.Bold = true;
        header.Style.Alignment.WrapText = true;
        worksheet.Row(HeaderRow).Height = 34;

        for (int rowNumber = HeaderRow + 1; rowNumber <= lastRow; rowNumber++)
        {
            worksheet.Range(rowNumber, 1, rowNumber, columns.Count).Style.Fill.BackgroundColor =
                (rowNumber - HeaderRow) % 2 == 1 ? Stripe : XLColor.White;
        }

        for (int index = 0; index < columns.Count; index++)
        {
            var definition = columns[index];
            int columnNumber = index + 1;
            var data = worksheet.Range(HeaderRow + 1, columnNumber, lastRow, columnNumber);
            data.Style.NumberFormat.Format = definition.Type switch
            {
                ExcelColumnType.Currency => "₱#,##0.00",
                ExcelColumnType.Integer => "0",
                ExcelColumnType.Date => "mmm dd, yyyy",
                // Callers supply fractional values, e.g. 0.05 for 5%.
                ExcelColumnType.Percentage => "0%",
                _ => "@"
            };
            data.Style.Alignment.Horizontal = definition.Type switch
            {
                ExcelColumnType.Currency or ExcelColumnType.Integer or ExcelColumnType.Percentage
                    => XLAlignmentHorizontalValues.Right,
                ExcelColumnType.Date or ExcelColumnType.Status => XLAlignmentHorizontalValues.Center,
                _ => XLAlignmentHorizontalValues.Left
            };

            if (definition.Type == ExcelColumnType.Status)
            {
                foreach (var cell in data.Cells())
                    StyleStatus(cell);
            }

            // Sample sizing keeps exports with many records responsive. Later long values wrap.
            var column = worksheet.Column(columnNumber);
            column.AdjustToContents(HeaderRow, Math.Min(lastRow, HeaderRow + WidthSampleRows));
            column.Width = Math.Clamp(column.Width + 2, definition.MinWidth, definition.MaxWidth);
            data.Style.Alignment.WrapText = true;
        }

        // Excel does not auto-fit wrapped row heights when opening generated workbooks.
        var graphics = DefaultGraphicEngine.Instance.Value;
        double digitWidth = graphics.GetMaxDigitWidth(workbook.Style.Font, 96);
        for (int rowNumber = HeaderRow + 1; rowNumber <= lastRow; rowNumber++)
        {
            int lines = 1;
            for (int columnNumber = 1; columnNumber <= columns.Count; columnNumber++)
            {
                var cell = worksheet.Cell(rowNumber, columnNumber);
                if (cell.DataType != XLDataType.Text) continue;
                double availablePixels = Math.Max(1, worksheet.Column(columnNumber).Width * digitWidth - 6);
                int cellLines = WrappedLineCount(cell.GetString(), availablePixels, cell.Style.Font, graphics);
                lines = Math.Max(lines, cellLines);
            }
            worksheet.Row(rowNumber).Height = Math.Min(409, Math.Max(24, lines * 15 + 9));
        }

        AddLogo(worksheet);
        worksheet.SheetView.FreezeRows(HeaderRow);
        worksheet.PageSetup.PageOrientation = columns.Count > 6
            ? XLPageOrientation.Landscape : XLPageOrientation.Portrait;
        worksheet.PageSetup.FitToPages(1, 0);
        worksheet.PageSetup.SetRowsToRepeatAtTop(1, HeaderRow);
        worksheet.PageSetup.PrintAreas.Add(1, 1, lastRow, columns.Count);

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static int WrappedLineCount(string text, double availablePixels, IXLFontBase font, IXLGraphicEngine graphics)
    {
        int lines = 0;
        foreach (string paragraph in text.Replace("\r", string.Empty).Split('\n'))
        {
            lines++;
            if (graphics.GetTextWidth(paragraph, font, 96) <= availablePixels) continue;
            double lineWidth = 0;
            foreach (Match match in Regex.Matches(paragraph, @"\S+\s*|\s+"))
            {
                double wordWidth = graphics.GetTextWidth(match.Value, font, 96);
                if (lineWidth > 0 && lineWidth + wordWidth > availablePixels)
                {
                    lines++;
                    lineWidth = 0;
                }
                if (wordWidth <= availablePixels)
                {
                    lineWidth += wordWidth;
                    continue;
                }

                // Excel also wraps inside an unbroken product code or email address.
                foreach (var rune in match.Value.EnumerateRunes())
                {
                    double width = graphics.GetTextWidth(rune.ToString(), font, 96);
                    if (lineWidth > 0 && lineWidth + width > availablePixels)
                    {
                        lines++;
                        lineWidth = 0;
                    }
                    lineWidth += width;
                }
            }
        }
        return lines;
    }

    private void AddLogo(IXLWorksheet worksheet)
    {
        var webRoot = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var path = Path.Combine(webRoot, "sources", "logo-svg.png");
        if (!File.Exists(path)) return;

        try
        {
            var picture = worksheet.AddPicture(path);
            picture.Name = "RJTech logo";
            var scale = Math.Min(175d / picture.OriginalWidth, 112d / picture.OriginalHeight);
            picture.WithPlacement(XLPicturePlacement.FreeFloating)
                .WithSize((int)(picture.OriginalWidth * scale), (int)(picture.OriginalHeight * scale))
                .MoveTo(worksheet.Cell(1, 1), 8, 8)
                .WithPlacement(XLPicturePlacement.FreeFloating);
        }
        catch (Exception exception)
        {
            // Branding is optional; an unavailable/corrupt asset must not prevent a download.
            if (worksheet.Pictures.TryGetPicture("RJTech logo", out var picture))
                picture.Delete();
            logger.LogWarning(exception, "The RJTech logo could not be added to the Excel export.");
        }
    }

    private static void StyleStatus(IXLCell cell)
    {
        var colors = cell.GetString().Trim().ToLowerInvariant() switch
        {
            "available" or "in stock" or "completed" or "paid" => ("#DDF0E2", "#20643A"),
            "low stock" or "pending" or "ongoing" => ("#FFF0C2", "#855B00"),
            "active" => ("#DBEBFA", "#215A87"),
            "out of stock" or "overdue" => ("#FBE0E2", "#A22632"),
            "cancelled" or "canceled" => ("#F5E0E2", "#963340"),
            "unavailable" or "refunded" or "archived" => ("#E8ECF0", "#535E6B"),
            _ => ((string?)null, (string?)null)
        };
        if (colors.Item1 == null || colors.Item2 == null) return;
        cell.Style.Fill.BackgroundColor = XLColor.FromHtml(colors.Item1);
        cell.Style.Font.FontColor = XLColor.FromHtml(colors.Item2);
        cell.Style.Font.Bold = true;
    }

    private static string SheetName(string title)
    {
        var name = string.Concat(title.Where(character => !"[]:*?/\\".Contains(character))).Trim('\'');
        return string.IsNullOrWhiteSpace(name) ? "Export" : name[..Math.Min(31, name.Length)];
    }
}
