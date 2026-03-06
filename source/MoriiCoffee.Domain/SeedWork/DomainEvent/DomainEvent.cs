namespace MoriiCoffee.Domain.SeedWork.DomainEvent;

/// <summary>Base record for all domain events.</summary>
public abstract record DomainEvent : IDomainEvent
{
    public Guid Id { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
