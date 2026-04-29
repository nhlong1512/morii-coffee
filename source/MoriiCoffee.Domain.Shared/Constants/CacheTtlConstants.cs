namespace MoriiCoffee.Domain.Shared.Constants;

/// <summary>
/// Centralized cache expiration values for MoriiCoffee.
/// </summary>
public static class CacheTtlConstants
{
    public static readonly TimeSpan Default = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan Cart = TimeSpan.FromHours(24);
}
