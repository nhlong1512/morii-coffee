using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MoriiCoffee.Application.SeedWork.DTOs.Banner;

/// <summary>Payload for creating a new promotional banner.</summary>
public class CreateBannerDto
{
    /// <summary>Main headline text (e.g., "Summer Refreshers are here").</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    /// <summary>Optional supporting subtitle text.</summary>
    [MaxLength(500)]
    public string? Subtitle { get; set; }

    /// <summary>Label for the call-to-action button (e.g., "Order Now").</summary>
    [MaxLength(100)]
    public string? Cta { get; set; }

    /// <summary>URL the call-to-action button navigates to.</summary>
    [MaxLength(500)]
    public string? CtaLink { get; set; }

    /// <summary>Sort order for displaying banners. Lower values appear first.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>UTC date/time when the banner becomes visible. Null means no start restriction.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>UTC date/time when the banner stops being visible. Null means no end restriction.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Whether the banner should be immediately active. Defaults to true.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Optional banner image file. Uploaded to S3 and stored as a CDN URL.</summary>
    public IFormFile? Image { get; set; }
}
