using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace MoriiCoffee.Application.SeedWork.Behaviors;

/// <summary>
/// MediatR pipeline behavior that warns when a request exceeds a performance threshold.
/// Requests taking longer than 500 ms are logged as warnings.
/// </summary>
public class PerformanceBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const int WarningThresholdMs = 500;
    private readonly ILogger<PerformanceBehavior<TRequest, TResponse>> _logger;

    public PerformanceBehavior(ILogger<PerformanceBehavior<TRequest, TResponse>> logger)
    {
        _logger = logger;
    }

    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var timer = Stopwatch.StartNew();
        TResponse response = await next();
        timer.Stop();

        long elapsed = timer.ElapsedMilliseconds;
        if (elapsed > WarningThresholdMs)
        {
            string requestName = typeof(TRequest).Name;
            _logger.LogWarning(
                "Long-running request detected: {RequestName} ({ElapsedMilliseconds} ms) {@Request}",
                requestName, elapsed, request);
        }

        return response;
    }
}
