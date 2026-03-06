using MoriiCoffee.Domain.SeedWork.DomainEvent;

namespace MoriiCoffee.Domain.SeedWork.AggregateRoot;

/// <summary>Marks a domain entity as an aggregate root capable of raising domain events.</summary>
public interface IAggregateRoot
{
    IReadOnlyCollection<IDomainEvent> GetDomainEvents();
    void ClearDomainEvents();
    void RaiseDomainEvent(IDomainEvent domainEvent);
}
