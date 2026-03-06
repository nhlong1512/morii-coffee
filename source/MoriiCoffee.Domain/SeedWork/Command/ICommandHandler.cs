using MediatR;

namespace MoriiCoffee.Domain.SeedWork.Command;

/// <summary>Handles a CQRS command of type <typeparamref name="TRequest"/> returning <typeparamref name="TResponse"/>.</summary>
public interface ICommandHandler<in TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : ICommand<TResponse>
{
}
