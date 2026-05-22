namespace MoriiCoffee.Application.SeedWork.DTOs.Report;

/// <summary>
/// Export payload returned from the report export query handler.
/// </summary>
public class AdminReportsExportDto
{
    /// <summary>Binary file content for the export.</summary>
    public byte[] Content { get; set; } = [];

    /// <summary>Export content type.</summary>
    public string ContentType { get; set; } = "text/csv";

    /// <summary>Suggested export file name.</summary>
    public string FileName { get; set; } = "admin-reports.csv";
}
