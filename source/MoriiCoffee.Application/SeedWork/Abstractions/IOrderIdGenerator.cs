namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Generates a unique, human-readable order number in the format <c>MRC-YYYYMMDD-NNN</c>
/// (e.g., <c>MRC-20260430-001</c>), where the sequence resets each calendar day.
/// </summary>
public interface IOrderIdGenerator
{
    /// <summary>
    /// Returns the next available order number for the current UTC date.
    /// The sequence number is derived from the count of existing orders placed today.
    /// </summary>
    Task<string> GenerateAsync();
}
