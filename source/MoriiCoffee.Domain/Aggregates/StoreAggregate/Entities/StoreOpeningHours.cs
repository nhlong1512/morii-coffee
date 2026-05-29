using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;

/// <summary>
/// Represents the opening hours for one day of the week for a <see cref="Store"/>.
/// Each store has exactly 7 records (0=Sunday through 6=Saturday).
/// When <see cref="IsClosed"/> is true, the open/close times are ignored.
/// </summary>
[Table("StoreOpeningHours")]
public class StoreOpeningHours : EntityBase
{
    /// <summary>Unique identifier for this opening hours record.</summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>Foreign key referencing the parent store.</summary>
    public Guid StoreId { get; set; }

    /// <summary>Navigation property to the parent store.</summary>
    public Store Store { get; set; } = null!;

    /// <summary>
    /// Day of the week this record represents.
    /// 0 = Sunday, 1 = Monday, … 6 = Saturday.
    /// </summary>
    public int DayOfWeek { get; set; }

    /// <summary>Opening time in 24-hour "HH:mm" format (e.g., "07:00"). Ignored when <see cref="IsClosed"/> is true.</summary>
    [MaxLength(5)]
    public string OpenTime { get; set; } = "07:00";

    /// <summary>Closing time in 24-hour "HH:mm" format (e.g., "22:00"). Ignored when <see cref="IsClosed"/> is true.</summary>
    [MaxLength(5)]
    public string CloseTime { get; set; } = "22:00";

    /// <summary>
    /// Indicates whether the store is closed on this day.
    /// When true, the store does not operate and open/close times are disregarded.
    /// </summary>
    public bool IsClosed { get; set; }

    /// <summary>Factory method to create a new opening hours record for a specific day.</summary>
    public static StoreOpeningHours Create(
        Guid storeId,
        int dayOfWeek,
        string openTime,
        string closeTime,
        bool isClosed)
        => new()
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            DayOfWeek = dayOfWeek,
            OpenTime = openTime,
            CloseTime = closeTime,
            IsClosed = isClosed,
        };

    /// <summary>Updates the operating window for this day without replacing the row.</summary>
    public StoreOpeningHours Update(
        string openTime,
        string closeTime,
        bool isClosed)
    {
        OpenTime = openTime;
        CloseTime = closeTime;
        IsClosed = isClosed;
        return this;
    }
}
