namespace MoriiCoffee.Domain.Shared.Settings;

/// <summary>
/// Configuration for order lifecycle automation.
/// Bound from the <c>OrderSettings</c> section in appsettings.json.
/// </summary>
public class OrderSettings
{
    /// <summary>
    /// Number of days after which an IN_DELIVERY order is automatically marked as DELIVERED.
    /// Default: 3 days.
    /// </summary>
    public int AutoCompleteAfterDays { get; set; } = 3;

    /// <summary>
    /// UTC hour (0–23) at which the auto-complete background job runs each day.
    /// Default: 2 (2:00 AM UTC).
    /// </summary>
    public int AutoCompleteJobRunHour { get; set; } = 2;
}
