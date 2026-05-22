namespace MoriiCoffee.Application.SeedWork.DTOs.Report;

/// <summary>
/// Revenue trend section returned to the admin reports client.
/// </summary>
public class RevenueSeriesDto
{
    /// <summary>Aggregated revenue-summary values for the selected range.</summary>
    public RevenueSeriesSummaryDto Summary { get; set; } = new();

    /// <summary>Time-bucketed revenue points.</summary>
    public List<RevenuePointDto> Points { get; set; } = [];
}

/// <summary>
/// Revenue summary values for the selected range.
/// </summary>
public class RevenueSeriesSummaryDto
{
    /// <summary>Total gross revenue for the selected range.</summary>
    public decimal GrossRevenue { get; set; }

    /// <summary>Total refund amount for the selected range.</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>Total net revenue for the selected range.</summary>
    public decimal NetRevenue { get; set; }

    /// <summary>Count of paid orders contributing to gross revenue.</summary>
    public int PaidOrders { get; set; }

    /// <summary>Average order value used in the summary display.</summary>
    public decimal AverageOrderValue { get; set; }

    /// <summary>Business currency code for the revenue values.</summary>
    public string Currency { get; set; } = "VND";
}

/// <summary>
/// One time bucket in the revenue trend series.
/// </summary>
public class RevenuePointDto
{
    /// <summary>Inclusive bucket start date.</summary>
    public DateOnly BucketStart { get; set; }

    /// <summary>Inclusive bucket end date.</summary>
    public DateOnly BucketEnd { get; set; }

    /// <summary>Human-readable bucket label.</summary>
    public string Label { get; set; } = null!;

    /// <summary>Gross revenue in this bucket.</summary>
    public decimal GrossRevenue { get; set; }

    /// <summary>Refund amount in this bucket.</summary>
    public decimal RefundAmount { get; set; }

    /// <summary>Net revenue in this bucket.</summary>
    public decimal NetRevenue { get; set; }

    /// <summary>Paid-order count in this bucket.</summary>
    public int PaidOrders { get; set; }
}
