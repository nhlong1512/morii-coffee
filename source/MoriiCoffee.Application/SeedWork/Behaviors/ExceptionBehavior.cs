using MediatR;
using Microsoft.Extensions.Logging;

namespace MoriiCoffee.Application.SeedWork.Behaviors;

/// <summary>
/// MediatR pipeline behavior that catches and logs unhandled exceptions.
/// Ensures all exceptions are consistently logged before being re-thrown.
/// </summary>
public class ExceptionBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<ExceptionBehavior<TRequest, TResponse>> _logger;

    public ExceptionBehavior(ILogger<ExceptionBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next();
        }
        catch (Exception ex)
        {
            string requestName = typeof(TRequest).Name;
            _logger.LogError(ex, "Unhandled exception for request {RequestName}: {@Request}", requestName, request);
            throw;
        }
    }
}
