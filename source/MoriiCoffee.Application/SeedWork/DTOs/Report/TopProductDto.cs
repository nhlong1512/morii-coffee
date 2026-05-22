namespace MoriiCoffee.Application.SeedWork.DTOs.Report;

/// <summary>
/// One ranked top-selling product returned in the dashboard response.
/// </summary>
public class TopProductDto
{
    /// <summary>Identifier of the product.</summary>
    public Guid ProductId { get; set; }

    /// <summary>Display name of the product.</summary>
    public string ProductName { get; set; } = null!;

    /// <summary>Optional thumbnail URL used by the admin UI.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Total units sold during the selected range.</summary>
    public int UnitsSold { get; set; }

    /// <summary>Count of distinct orders contributing to the sales total.</summary>
    public int OrderCount { get; set; }

    /// <summary>Gross product revenue for the selected range.</summary>
    public decimal GrossRevenue { get; set; }
}
