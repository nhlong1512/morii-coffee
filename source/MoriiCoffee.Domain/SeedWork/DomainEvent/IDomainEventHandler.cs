using MediatR;

namespace MoriiCoffee.Domain.SeedWork.DomainEvent;

/// <summary>Handles a specific domain event of type <typeparamref name="T"/>.</summary>
public interface IDomainEventHandler<in T> : INotificationHandler<T>
    where T : IDomainEvent
{
}
