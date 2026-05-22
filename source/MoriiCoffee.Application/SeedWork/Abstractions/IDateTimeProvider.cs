namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>
/// Provides the current UTC date and time. Abstracted for deterministic range calculations in tests.
/// </summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}
