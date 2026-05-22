namespace MoriiCoffee.Application.SeedWork.DTOs.Store;

/// <summary>Read model for a single day's opening hours record.</summary>
public class StoreOpeningHoursDto
{
    /// <summary>Unique identifier of this opening hours record.</summary>
    public Guid Id { get; set; }

    /// <summary>Day of week: 0 = Sunday, 1 = Monday, … 6 = Saturday.</summary>
    public int DayOfWeek { get; set; }

    /// <summary>Opening time in 24-hour "HH:mm" format.</summary>
    public string OpenTime { get; set; } = null!;

    /// <summary>Closing time in 24-hour "HH:mm" format.</summary>
    public string CloseTime { get; set; } = null!;

    /// <summary>True if the store does not operate on this day.</summary>
    public bool IsClosed { get; set; }
}
