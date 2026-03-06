using MediatR;

namespace MoriiCoffee.Domain.SeedWork.DomainEvent;

/// <summary>Marker interface for all domain events.</summary>
public interface IDomainEvent : INotification
{
}
