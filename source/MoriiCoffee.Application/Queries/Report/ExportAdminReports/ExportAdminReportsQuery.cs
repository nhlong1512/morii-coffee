using MoriiCoffee.Application.SeedWork.DTOs.Report;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Report.ExportAdminReports;

/// <summary>
/// Query for exporting the currently selected admin reports view as CSV.
/// </summary>
public class ExportAdminReportsQuery : IQuery<AdminReportsExportDto>
{
    public ExportAdminReportsQuery(
        string? format,
        string? preset,
        DateOnly? from,
        DateOnly? to,
        string? granularity,
        string? timezone)
    {
        Format = format;
        Preset = preset;
        From = from;
        To = to;
        Granularity = granularity;
        Timezone = timezone;
    }

    public string? Format { get; }

    public string? Preset { get; }

    public DateOnly? From { get; }

    public DateOnly? To { get; }

    public string? Granularity { get; }

    public string? Timezone { get; }
}
