namespace MoriiCoffee.Application.SeedWork.DTOs.Store;

/// <summary>
/// Input payload for creating or fully updating a store.
/// Used by both POST (create) and PUT (full update) endpoints.
/// </summary>
public class CreateStoreDto
{
    /// <summary>Display name of the store. Must be unique.</summary>
    public string Name { get; set; } = null!;

    /// <summary>Optional URL slug. Auto-generated from <see cref="Name"/> if null or empty.</summary>
    public string? Slug { get; set; }

    /// <summary>Street address.</summary>
    public string Address { get; set; } = null!;

    /// <summary>Optional district or ward.</summary>
    public string? District { get; set; }

    /// <summary>City where the store is located.</summary>
    public string City { get; set; } = null!;

    /// <summary>Optional province or region.</summary>
    public string? Province { get; set; }

    /// <summary>Geographic latitude (-90 to 90).</summary>
    public double Latitude { get; set; }

    /// <summary>Geographic longitude (-180 to 180).</summary>
    public double Longitude { get; set; }

    /// <summary>Customer-facing phone number.</summary>
    public string Phone { get; set; } = null!;

    /// <summary>Optional store email address.</summary>
    public string? Email { get; set; }

    /// <summary>Optional full URL of the store's cover image.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Whether the store should be immediately visible on the public locator.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Sort position on the public page.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>
    /// Opening hours for each day of the week.
    /// Must contain exactly 7 items with unique <c>DayOfWeek</c> values (0–6).
    /// </summary>
    public List<CreateStoreOpeningHoursDto> OpeningHours { get; set; } = new();
}

/// <summary>Opening hours data for a single day of the week.</summary>
public class CreateStoreOpeningHoursDto
{
    /// <summary>Day of week: 0 = Sunday … 6 = Saturday.</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Opening time in 24-hour "HH:mm" format.</summary>
    public string OpenTime { get; set; } = "07:00";

    /// <summary>Closing time in 24-hour "HH:mm" format.</summary>
    public string CloseTime { get; set; } = "22:00";

    /// <summary>True if the store does not operate on this day.</summary>
    public bool IsClosed { get; set; }
}
