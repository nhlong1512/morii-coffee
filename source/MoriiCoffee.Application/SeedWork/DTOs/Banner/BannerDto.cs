namespace MoriiCoffee.Application.SeedWork.DTOs.Banner;

/// <summary>
/// Read model returned by all banner endpoints.
/// Contains the full banner data including the CDN image URL.
/// </summary>
public class BannerDto
{
    /// <summary>Unique identifier of the banner.</summary>
    public Guid Id { get; set; }

    /// <summary>Main headline text.</summary>
    public string Title { get; set; } = null!;

    /// <summary>Optional supporting subtitle text.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Label for the call-to-action button.</summary>
    public string? Cta { get; set; }

    /// <summary>URL the call-to-action button navigates to.</summary>
    public string? CtaLink { get; set; }

    /// <summary>Public CDN URL of the banner image. Null until an image is uploaded.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Sort order for displaying banners (ascending).</summary>
    public int DisplayOrder { get; set; }

    /// <summary>UTC date/time when the banner becomes visible.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>UTC date/time when the banner stops being visible.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Whether this banner is currently active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Timestamp when the banner was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Timestamp of the last update.</summary>
    public DateTime? UpdatedAt { get; set; }
}
