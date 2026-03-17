using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;

namespace MoriiCoffee.Domain.Aggregates.BannerAggregate;

/// <summary>
/// Represents a promotional banner displayed on the storefront.
/// Banners are ordered by <see cref="DisplayOrder"/> and can be toggled active/inactive
/// without deletion. Supports soft-delete and MinIO-backed image storage.
/// </summary>
[Table("Banners")]
public class Banner : AggregateRoot
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Headline text shown on the banner (e.g., "Summer Special — 20% Off").</summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string Title { get; set; } = null!;

    /// <summary>Optional supporting copy displayed beneath the title.</summary>
    [MaxLength(1000)]
    [Column(TypeName = "nvarchar(1000)")]
    public string? Description { get; set; }

    /// <summary>Full public URL of the banner image served from MinIO.</summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? ImageUrl { get; set; }

    /// <summary>Internal MinIO object key for the banner image — used to delete the old image on update.</summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? ImageFileName { get; set; }

    /// <summary>Whether the banner is currently visible to customers on the storefront.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Sort position of the banner in the storefront carousel. Lower values appear first.</summary>
    public int DisplayOrder { get; set; }

    #region Domain Methods

    /// <summary>Makes the banner visible on the storefront.</summary>
    public void Activate() => IsActive = true;

    /// <summary>Hides the banner from the storefront without deleting it.</summary>
    public void Deactivate() => IsActive = false;

    /// <summary>Toggles the banner visibility between active and inactive.</summary>
    public void ToggleStatus() => IsActive = !IsActive;

    /// <summary>Updates the banner's position in the display order.</summary>
    public void Reorder(int displayOrder) => DisplayOrder = displayOrder;

    /// <summary>Replaces the banner image reference after a new upload to MinIO.</summary>
    public void SetImage(string? url, string? fileName)
    {
        ImageUrl = url;
        ImageFileName = fileName;
    }

    #endregion
}
