using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Infrastructure.BackgroundJobs;

/// <summary>
/// Hangfire recurring job that runs once per day and automatically marks stale IN_DELIVERY
/// orders as DELIVERED. An order is considered stale when its <c>CreatedAt</c> is older than
/// <see cref="OrderSettings.AutoCompleteAfterDays"/> days.
/// Hangfire creates a fresh DI scope for each execution, so <see cref="IUnitOfWork"/> can
/// be injected directly without <c>IServiceScopeFactory</c>.
/// </summary>
public class OrderAutoCompleteJob
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly OrderSettings _settings;
    private readonly ILogger<OrderAutoCompleteJob> _logger;

    public OrderAutoCompleteJob(
        IUnitOfWork unitOfWork,
        OrderSettings settings,
        ILogger<OrderAutoCompleteJob> logger)
    {
        _unitOfWork = unitOfWork;
        _settings = settings;
        _logger = logger;
    }

    /// <summary>Entry point called by Hangfire on each scheduled execution.</summary>
    [DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
    public async Task ExecuteAsync(CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var cutoffDate = now.AddDays(-_settings.AutoCompleteAfterDays);

        _logger.LogInformation(
            "OrderAutoCompleteJob [{Now:u}]: scanning for IN_DELIVERY orders created before {Cutoff:u}.",
            now, cutoffDate);

        // trackChanges: true — EF detects OrderStatus changes automatically, no explicit Update() needed
        var staleOrders = await _unitOfWork.Orders
            .FindByCondition(
                o => !o.IsDeleted
                     && o.OrderStatus == EOrderStatus.IN_DELIVERY
                     && o.CreatedAt <= cutoffDate,
                trackChanges: true)
            .ToListAsync(ct);

        if (staleOrders.Count == 0)
        {
            _logger.LogInformation("OrderAutoCompleteJob [{Now:u}]: no stale IN_DELIVERY orders found.", now);
            return;
        }

        var completed = 0;
        var skipped = 0;
        foreach (var order in staleOrders)
        {
            try
            {
                order.MarkDelivered();
                completed++;
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "OrderAutoCompleteJob: skipping order {OrderId} — {Reason}.", order.Id, ex.Message);
                skipped++;
            }
        }

        await _unitOfWork.CommitAsync();

        _logger.LogInformation(
            "OrderAutoCompleteJob [{Now:u}]: run complete — completed {Completed}, skipped {Skipped} of {Total} candidate order(s).",
            now, completed, skipped, staleOrders.Count);
    }
}
