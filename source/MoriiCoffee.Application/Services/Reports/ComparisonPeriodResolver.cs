using MoriiCoffee.Domain.Repositories;

namespace MoriiCoffee.Application.Services.Reports;

/// <summary>
/// Resolves the immediately preceding comparison period for a selected reporting range.
/// </summary>
public class ComparisonPeriodResolver
{
    /// <summary>
    /// Returns the previous period with the same inclusive day count as the selected range.
    /// </summary>
    public AdminReportComparisonRange Resolve(DateOnly from, DateOnly to)
    {
        var lengthInDays = to.DayNumber - from.DayNumber + 1;
        var comparisonTo = from.AddDays(-1);
        var comparisonFrom = comparisonTo.AddDays(-(lengthInDays - 1));

        return new AdminReportComparisonRange(comparisonFrom, comparisonTo);
    }
}

/// <summary>
/// Represents the comparison period paired with a selected reporting range.
/// </summary>
public sealed record AdminReportComparisonRange(DateOnly From, DateOnly To);
