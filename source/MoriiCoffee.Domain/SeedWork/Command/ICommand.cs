using MediatR;

namespace MoriiCoffee.Domain.SeedWork.Command;

/// <summary>Marker interface for CQRS commands that return a result of type <typeparamref name="TResponse"/>.</summary>
public interface ICommand<TResponse> : IRequest<TResponse>
{
}
