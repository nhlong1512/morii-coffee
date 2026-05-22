namespace MoriiCoffee.Application.SeedWork.DTOs.Store;

/// <summary>
/// Read model returned by all store queries (public and admin).
/// The <see cref="DistanceKm"/> field is populated only when geolocation parameters are provided.
/// </summary>
public class StoreDto
{
    /// <summary>Unique store identifier.</summary>
    public Guid Id { get; set; }

    /// <summary>Display name of the store.</summary>
    public string Name { get; set; } = null!;

    /// <summary>URL-safe slug for the store.</summary>
    public string Slug { get; set; } = null!;

    /// <summary>Street address.</summary>
    public string Address { get; set; } = null!;

    /// <summary>Optional district or ward.</summary>
    public string? District { get; set; }

    /// <summary>City where the store is located.</summary>
    public string City { get; set; } = null!;

    /// <summary>Optional province or region.</summary>
    public string? Province { get; set; }

    /// <summary>Geographic latitude.</summary>
    public double Latitude { get; set; }

    /// <summary>Geographic longitude.</summary>
    public double Longitude { get; set; }

    /// <summary>Customer-facing phone number.</summary>
    public string Phone { get; set; } = null!;

    /// <summary>Optional store email address.</summary>
    public string? Email { get; set; }

    /// <summary>Optional URL of the store's cover image.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Whether the store is currently visible on the public locator page.</summary>
    public bool IsActive { get; set; }

    /// <summary>Sort position on the public page; lower values appear first.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Distance in kilometers from the user's provided coordinates.
    /// Null when no geolocation parameters were supplied in the request.
    /// </summary>
    public double? DistanceKm { get; set; }

    /// <summary>
    /// The 7 opening hours records — one per day of week (0=Sunday … 6=Saturday).
    /// </summary>
    public List<StoreOpeningHoursDto> OpeningHours { get; set; } = new();

    /// <summary>UTC timestamp when the store record was created.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>UTC timestamp of the last update, or null if never updated.</summary>
    public DateTime? UpdatedAt { get; set; }
}
