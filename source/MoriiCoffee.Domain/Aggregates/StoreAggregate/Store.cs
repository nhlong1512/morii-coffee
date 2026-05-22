using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;

namespace MoriiCoffee.Domain.Aggregates.StoreAggregate;

/// <summary>
/// Represents a physical Morii Coffee branch location.
/// Acts as the aggregate root for the Store bounded context.
/// Each store has exactly 7 <see cref="StoreOpeningHours"/> child records (one per day of week).
/// </summary>
[Table("Stores")]
public class Store : AggregateRoot
{
    /// <summary>Unique identifier for the store.</summary>
    [Key]
    public Guid Id { get; private set; }

    /// <summary>Display name of the store location (e.g., "Morii Coffee - District 1"). Must be unique.</summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; private set; } = null!;

    /// <summary>URL-safe slug derived from the store name. Auto-generated if not provided. Must be unique.</summary>
    [Required]
    [MaxLength(200)]
    public string Slug { get; private set; } = null!;

    /// <summary>Street address of the store.</summary>
    [Required]
    [MaxLength(500)]
    public string Address { get; private set; } = null!;

    /// <summary>Optional district or ward within the city.</summary>
    [MaxLength(100)]
    public string? District { get; private set; }

    /// <summary>City where the store is located. Used for city-based filtering.</summary>
    [Required]
    [MaxLength(100)]
    public string City { get; private set; } = null!;

    /// <summary>Optional province or region (for stores outside major cities).</summary>
    [MaxLength(100)]
    public string? Province { get; private set; }

    /// <summary>Geographic latitude coordinate. Required for map rendering and geolocation sort.</summary>
    public double Latitude { get; private set; }

    /// <summary>Geographic longitude coordinate. Required for map rendering and geolocation sort.</summary>
    public double Longitude { get; private set; }

    /// <summary>Customer-facing phone number for the store.</summary>
    [Required]
    [MaxLength(20)]
    public string Phone { get; private set; } = null!;

    /// <summary>Optional email address for the store.</summary>
    [MaxLength(100)]
    public string? Email { get; private set; }

    /// <summary>Optional URL of the store's cover image (full URL, stored verbatim).</summary>
    [MaxLength(500)]
    public string? CoverImageUrl { get; private set; }

    /// <summary>
    /// Indicates whether the store is visible on the public store locator page.
    /// Inactive stores remain visible in admin views.
    /// </summary>
    public bool IsActive { get; private set; } = true;

    /// <summary>Controls the sort order of this store on the public page. Lower values appear first.</summary>
    public int DisplayOrder { get; private set; }

    /// <summary>
    /// The 7 opening hours records for this store — one per day of week (0=Sunday … 6=Saturday).
    /// Always contains exactly 7 entries.
    /// </summary>
    public ICollection<StoreOpeningHours> OpeningHours { get; set; } = new List<StoreOpeningHours>();

    /// <summary>
    /// Creates a new Store aggregate from the provided DTO and pre-computed slug.
    /// </summary>
    public static Store Create(CreateStoreData data, string slug) => new()
    {
        Id = Guid.NewGuid(),
        Name = data.Name,
        Slug = slug,
        Address = data.Address,
        District = data.District,
        City = data.City,
        Province = data.Province,
        Latitude = data.Latitude,
        Longitude = data.Longitude,
        Phone = data.Phone,
        Email = data.Email,
        CoverImageUrl = data.CoverImageUrl,
        IsActive = data.IsActive,
        DisplayOrder = data.DisplayOrder,
    };

    /// <summary>Updates all mutable fields of the store. Opening hours are managed separately.</summary>
    public Store Update(CreateStoreData data, string slug)
    {
        Name = data.Name;
        Slug = slug;
        Address = data.Address;
        District = data.District;
        City = data.City;
        Province = data.Province;
        Latitude = data.Latitude;
        Longitude = data.Longitude;
        Phone = data.Phone;
        Email = data.Email;
        CoverImageUrl = data.CoverImageUrl;
        IsActive = data.IsActive;
        DisplayOrder = data.DisplayOrder;
        return this;
    }

    /// <summary>Sets the store's active/inactive status without a full update.</summary>
    public Store SetStatus(bool isActive)
    {
        IsActive = isActive;
        return this;
    }

    /// <summary>Sets the display order for sorting on the public page.</summary>
    public Store SetDisplayOrder(int order)
    {
        DisplayOrder = order;
        return this;
    }
}

/// <summary>
/// Flat data container used by both Create and Update operations to carry store field values.
/// Avoids duplicating a separate UpdateStoreData type since the fields are identical.
/// </summary>
public record CreateStoreData(
    string Name,
    string? Slug,
    string Address,
    string? District,
    string City,
    string? Province,
    double Latitude,
    double Longitude,
    string Phone,
    string? Email,
    string? CoverImageUrl,
    bool IsActive,
    int DisplayOrder
);
