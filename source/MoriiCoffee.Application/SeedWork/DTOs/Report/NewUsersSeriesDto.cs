namespace MoriiCoffee.Application.SeedWork.DTOs.Report;

/// <summary>
/// New-user growth section returned to the reports client.
/// </summary>
public class NewUsersSeriesDto
{
    /// <summary>Total number of users created during the selected range.</summary>
    public int TotalNewUsers { get; set; }

    /// <summary>Time-bucketed user growth points.</summary>
    public List<NewUserPointDto> Points { get; set; } = [];
}

/// <summary>
/// One time bucket in the new-user growth trend.
/// </summary>
public class NewUserPointDto
{
    /// <summary>Inclusive bucket start date.</summary>
    public DateOnly BucketStart { get; set; }

    /// <summary>Inclusive bucket end date.</summary>
    public DateOnly BucketEnd { get; set; }

    /// <summary>Human-readable bucket label.</summary>
    public string Label { get; set; } = null!;

    /// <summary>Number of users created in this bucket.</summary>
    public int Users { get; set; }
}
