using Hangfire;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Infrastructure.BackgroundJobs;

namespace MoriiCoffee.Presentation.Extensions;

/// <summary>
/// Registers all Hangfire recurring jobs at application startup.
/// Add new recurring jobs here as the application grows.
/// </summary>
internal static class HangfireJobsExtensions
{
    public static void RegisterRecurringJobs(this WebApplication app)
    {
        var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
        var orderSettings = app.Services.GetRequiredService<OrderSettings>();

        recurringJobs.AddOrUpdate<OrderAutoCompleteJob>(
            "order-auto-complete",
            job => job.ExecuteAsync(CancellationToken.None),
            $"0 {orderSettings.AutoCompleteJobRunHour} * * *");
    }
}
