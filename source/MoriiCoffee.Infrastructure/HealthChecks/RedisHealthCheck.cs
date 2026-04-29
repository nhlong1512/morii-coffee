using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;

namespace MoriiCoffee.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies Redis connectivity using the shared multiplexer.
/// </summary>
public class RedisHealthCheck : IHealthCheck
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;

    public RedisHealthCheck(IConnectionMultiplexer connectionMultiplexer)
    {
        _connectionMultiplexer = connectionMultiplexer;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IDatabase database = _connectionMultiplexer.GetDatabase();
            RedisResult pingResult = await database.ExecuteAsync("PING");

            return pingResult.ToString() == "PONG"
                ? HealthCheckResult.Healthy("Redis is reachable.")
                : HealthCheckResult.Unhealthy("Redis responded unexpectedly.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Redis health check failed.", ex);
        }
    }
}
