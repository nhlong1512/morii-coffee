using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.BannerAggregate;

/// <summary>
/// Represents a promotional banner displayed on the storefront (e.g., hero slides, seasonal offers).
/// Acts as the aggregate root for the Banner bounded context.
/// The banner image is stored as a CDN URL directly on the entity.
/// </summary>
[Table("Banners")]
public class Banner : AggregateRoot
{
    /// <summary>Primary key.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Main headline text shown on the banner (e.g., "Summer Refreshers are here").</summary>
    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = null!;

    /// <summary>Optional supporting text shown beneath the title.</summary>
    [MaxLength(500)]
    public string? Subtitle { get; set; }

    /// <summary>Label for the call-to-action button (e.g., "Order Now").</summary>
    [MaxLength(100)]
    public string? Cta { get; set; }

    /// <summary>URL the call-to-action button navigates to.</summary>
    [MaxLength(500)]
    public string? CtaLink { get; set; }

    /// <summary>Public CDN URL of the banner image. Null until an image is uploaded.</summary>
    [MaxLength(500)]
    public string? ImageUrl { get; set; }

    /// <summary>Sort order for displaying banners. Lower values appear first.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>UTC date/time when the banner becomes visible. Null means no start restriction.</summary>
    public DateTime? StartDate { get; set; }

    /// <summary>UTC date/time when the banner stops being visible. Null means no end restriction.</summary>
    public DateTime? EndDate { get; set; }

    /// <summary>Whether this banner is currently active and eligible to be displayed.</summary>
    public bool IsActive { get; set; } = true;
}
