namespace MoriiCoffee.Application.SeedWork.DTOs.Report;

/// <summary>
/// Reporting-period metadata returned to dashboard and export consumers.
/// </summary>
public class ReportRangeDto
{
    /// <summary>Inclusive start date of the current range.</summary>
    public DateOnly From { get; set; }

    /// <summary>Inclusive end date of the current range.</summary>
    public DateOnly To { get; set; }

    /// <summary>Requested preset identifier when applicable.</summary>
    public string? Preset { get; set; }

    /// <summary>Normalized bucket granularity.</summary>
    public string Granularity { get; set; } = null!;

    /// <summary>Timezone used to derive local buckets.</summary>
    public string Timezone { get; set; } = null!;

    /// <summary>Inclusive start date of the comparison range.</summary>
    public DateOnly ComparisonFrom { get; set; }

    /// <summary>Inclusive end date of the comparison range.</summary>
    public DateOnly ComparisonTo { get; set; }
}
