using MediatR;

namespace MoriiCoffee.Domain.SeedWork.Query;

/// <summary>Marker interface for CQRS queries that return a result of type <typeparamref name="TResponse"/>.</summary>
public interface IQuery<TResponse> : IRequest<TResponse>
{
}
