namespace MoriiCoffee.Application.SeedWork.DTOs.Report;

/// <summary>
/// One summary metric card returned in the reports dashboard payload.
/// </summary>
public class ReportMetricCardDto
{
    /// <summary>Current metric value.</summary>
    public decimal Value { get; set; }

    /// <summary>Previous metric value for the comparison period when available.</summary>
    public decimal? PreviousValue { get; set; }

    /// <summary>Percentage change relative to the comparison period when available.</summary>
    public decimal? ChangePercent { get; set; }

    /// <summary>Direction label for frontend presentation.</summary>
    public string? ChangeDirection { get; set; }

    /// <summary>Whether the metric supports a period-over-period comparison.</summary>
    public bool ComparisonSupported { get; set; }
}
