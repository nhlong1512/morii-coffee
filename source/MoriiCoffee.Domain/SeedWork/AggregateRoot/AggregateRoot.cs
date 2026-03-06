using MoriiCoffee.Domain.SeedWork.DomainEvent;
using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.SeedWork.AggregateRoot;

/// <summary>
/// Base class for all aggregate roots.
/// Manages a collection of domain events that can be raised and cleared.
/// </summary>
public abstract class AggregateRoot : EntityBase, IAggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot() { }

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();

    public void ClearDomainEvents() => _domainEvents.Clear();

    public void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);
}
