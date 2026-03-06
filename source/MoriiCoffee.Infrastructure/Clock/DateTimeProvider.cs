namespace MoriiCoffee.Infrastructure.Clock;

/// <summary>Provides the current UTC date and time. Abstracted for testability.</summary>
public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}

public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
