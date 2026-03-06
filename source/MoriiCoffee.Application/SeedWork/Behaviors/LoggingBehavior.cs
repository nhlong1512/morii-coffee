using MediatR;
using Microsoft.Extensions.Logging;

namespace MoriiCoffee.Application.SeedWork.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the start and end of every request.
/// </summary>
public class LoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger;

    public LoggingBehavior(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("BEGIN: {RequestName}Handler", typeof(TRequest).Name);
        TResponse response = await next();
        _logger.LogInformation("END: {RequestName}Handler", typeof(TRequest).Name);
        return response;
    }
}
