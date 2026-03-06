using MediatR;

namespace MoriiCoffee.Domain.SeedWork.Query;

/// <summary>Handles a CQRS query of type <typeparamref name="TRequest"/> returning <typeparamref name="TResponse"/>.</summary>
public interface IQueryHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IQuery<TResponse>
{
}
