using System.Text.Json.Serialization;

namespace Capstone_RJTech.Models;

/// <summary>
/// IDs of every row matching the page filters, before pagination; null requests all records.
/// Requiring the JSON property prevents an incomplete filter request from exporting all data.
/// </summary>
public sealed class ExcelExportSelection
{
    [JsonRequired]
    public int[]? RecordIds { get; set; }
}
