using MoriiCoffee.Application.SeedWork.DTOs.Report;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Report.GetAdminReportsDashboard;

/// <summary>
/// Query for loading the admin reports dashboard for a normalized or preset reporting range.
/// </summary>
public class GetAdminReportsDashboardQuery : IQuery<AdminReportsDashboardDto>
{
    public GetAdminReportsDashboardQuery(
        string? preset,
        DateOnly? from,
        DateOnly? to,
        string? granularity,
        string? timezone)
    {
        Preset = preset;
        From = from;
        To = to;
        Granularity = granularity;
        Timezone = timezone;
    }

    public string? Preset { get; }

    public DateOnly? From { get; }

    public DateOnly? To { get; }

    public string? Granularity { get; }

    public string? Timezone { get; }
}
