using MoriiCoffee.Application.SeedWork.Abstractions;

namespace MoriiCoffee.Infrastructure.Clock;

/// <summary>Default wall-clock implementation used by infrastructure and application services.</summary>
public class DateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}
