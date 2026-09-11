namespace Capstone_RJTech.Services;

public enum ExcelColumnType
{
    Text,
    Integer,
    Currency,
    Date,
    Percentage,
    Status
}

/// <summary>Display metadata only; values remain typed numbers, dates, or text.</summary>
public sealed record ExcelExportColumn(
    string Header,
    ExcelColumnType Type = ExcelColumnType.Text,
    double MinWidth = 12,
    double MaxWidth = 40);

public interface IExcelExportService
{
    byte[] ExportToExcel(
        string title,
        IReadOnlyList<ExcelExportColumn> columns,
        IEnumerable<IReadOnlyList<object?>> rows);
}

public static class ExcelExportFile
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static string CreateFileName(string module)
    {
        // RJTech operates in the Philippines; filenames should not depend on server timezone.
        var timestamp = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(8));
        var safeModule = string.Concat(module.Where(character => char.IsAsciiLetterOrDigit(character)));
        return $"RJTech_{safeModule}_{timestamp:yyyy-MM-dd_HHmmss_fff}.xlsx";
    }
}
