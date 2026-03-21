using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace MoriiCoffee.Application.SeedWork.DTOs.Banner;

/// <summary>Payload for updating an existing promotional banner.</summary>
public class UpdateBannerDto
{
    /// <summary>Updated headline text.</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    /// <summary>Updated supporting subtitle text.</summary>
    [MaxLength(500)]
    public string? Subtitle { get; set; }

    /// <summary>Updated call-to-action label.</summary>
    [MaxLength(100)]
    public string? Cta { get; set; }

    /// <summary>Updated call-to-action URL.</summary>
    [MaxLength(500)]
    public string? CtaLink { get; set; }

    /// <summary>Updated display order.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Updated start date (UTC). Null removes the restriction.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>Updated end date (UTC). Null removes the restriction.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Updated active flag.</summary>
    public bool IsActive { get; set; }

    /// <summary>Optional replacement banner image. If provided, replaces the current CDN URL.</summary>
    public IFormFile? Image { get; set; }
}
