# Change Summary — Migrate Background Job to Hangfire (ENG)

**Feature:** Background Job — OrderAutoComplete  
**Branch:** `009-cart-payment`  
**Date:** 2026-05-02  

---

## 1. Purpose

Phase 6 originally implemented `OrderAutoCompleteJob` as a .NET `BackgroundService`. While sufficient for a single job, the project will need additional background jobs in the future (e.g., order confirmation emails, daily summary emails). The decision was made to migrate to **Hangfire** early to:

- Get **automatic retry** on job failure (critical for email jobs with transient network errors)
- Get a **dashboard** to monitor and manage job execution
- Make adding new jobs significantly easier — no manual scheduling loop needed
- Persist job execution history for audit trail

**Why SQL Server (MoriiCoffeeDb) as Hangfire storage:**  
Hangfire requires a persistence store for job state. At Morii Coffee's current scale (tens of orders per day), sharing the existing database avoids extra infrastructure overhead. Hangfire creates its own `[HangFire]` schema and never touches application tables.

**Redis was not chosen** because Redis is in-memory: without explicit persistence configuration, a restart wipes all job history — unacceptable for order/email jobs that require an audit trail.

---

## 2. New Files

### `source/MoriiCoffee.Infrastructure/Configurations/HangfireConfiguration.cs`

Extension method `ConfigureHangfire` that registers Hangfire into the DI container:

```csharp
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions { ... }));

services.AddHangfireServer();
```

- `UseSqlServerStorage` — reads `DefaultConnectionString` from `appsettings.json`; automatically creates the `[HangFire]` schema on first startup
- `AddHangfireServer` — starts the Hangfire worker process inside the application to listen for and execute queued jobs

---

## 3. Modified Files

### `source/MoriiCoffee.Infrastructure/BackgroundJobs/OrderAutoCompleteJob.cs`

**Before (BackgroundService):**
- Inherited `BackgroundService`, overrode `ExecuteAsync`
- Contained a `while (!stoppingToken.IsCancellationRequested)` loop computing `nextRun`
- Required `IServiceScopeFactory` because `BackgroundService` is singleton while `IUnitOfWork` is scoped
- Manually computed delay: `todayRun = now.Date.AddHours(hour)` → `Task.Delay(nextRun - now)`

**After (Hangfire job):**
- Plain class, **no inheritance**
- Injects `IUnitOfWork` directly — Hangfire creates a fresh DI scope per execution
- No scheduling loop, no `IServiceScopeFactory`, no manual `nextRun` computation
- Business logic is fully preserved:
  - Queries `IN_DELIVERY` orders with `CreatedAt <= cutoffDate`
  - Calls `order.MarkDelivered()` on the aggregate
  - Single `CommitAsync()` for the entire batch

### `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`

- **Removed:** `services.AddHostedService<OrderAutoCompleteJob>()` — hosted service no longer used
- **Removed:** `using MoriiCoffee.Infrastructure.BackgroundJobs` — namespace no longer needed here
- **Added:** `services.ConfigureHangfire(configuration)` — calls the new extension method

### `source/MoriiCoffee.Presentation/Extensions/ApplicationExtensions.cs`

Two new blocks added to the HTTP pipeline (after `MapControllers`):

**Block 1 — Hangfire Dashboard:**
```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new LocalRequestsOnlyAuthorizationFilter()]
});
```
Dashboard is only accessible from localhost. In production, add protection via reverse proxy or VPN.

**Block 2 — Register Recurring Job:**
```csharp
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
var orderSettings = app.Services.GetRequiredService<OrderSettings>();
recurringJobs.AddOrUpdate<OrderAutoCompleteJob>(
    "order-auto-complete",
    job => job.ExecuteAsync(CancellationToken.None),
    $"0 {orderSettings.AutoCompleteJobRunHour} * * *");
```
- Job ID `"order-auto-complete"` — unique identifier visible in the Hangfire dashboard
- CRON `"0 2 * * *"` (with `AutoCompleteJobRunHour = 2`) — runs at 02:00 UTC daily
- On app restart, `AddOrUpdate` updates the existing schedule instead of creating duplicates

---

## 4. Packages Added

| Package | Version | Project |
|---|---|---|
| `Hangfire.Core` | 1.8.23 | Infrastructure |
| `Hangfire.SqlServer` | 1.8.23 | Infrastructure |
| `Hangfire.AspNetCore` | 1.8.23 | Infrastructure + Presentation |

`Hangfire.AspNetCore` is required in Infrastructure because the `AddHangfire` and `AddHangfireServer` extension methods are defined in that package (via `Hangfire.NetCore`).

---

## 5. Database Changes

**No EF Core migration required.** Hangfire manages its own schema independently.

On first startup, Hangfire automatically creates 7 tables under the `[HangFire]` schema:

| Table | Purpose |
|---|---|
| `[HangFire].[Job]` | Each job instance (type, args, current state) |
| `[HangFire].[JobParameter]` | Parameters for each job |
| `[HangFire].[JobQueue]` | Queue of jobs waiting to be processed |
| `[HangFire].[State]` | State transition history (Enqueued → Processing → Succeeded/Failed) |
| `[HangFire].[Counter]` | Aggregate statistics (succeeded, failed counts) |
| `[HangFire].[Hash]` | Recurring job config (CRON expression, last run time) |
| `[HangFire].[Set]` | Internal Hangfire data structures |

---

## 6. API Changes

No new API endpoints. The Hangfire dashboard is a standalone web UI:

- **URL:** `http://localhost:{port}/hangfire`
- **Access:** Localhost only (`LocalRequestsOnlyAuthorizationFilter`)

---

## 7. Adding New Jobs in the Future

To add a new background job (e.g., daily email summary):

**Step 1** — Create the job class in `MoriiCoffee.Infrastructure/BackgroundJobs/`:
```csharp
public class DailyEmailSummaryJob
{
    public DailyEmailSummaryJob(IEmailService emailService) { ... }
    public async Task ExecuteAsync() { ... }
}
```

**Step 2** — Register the recurring job in `ApplicationExtensions.cs`:
```csharp
recurringJobs.AddOrUpdate<DailyEmailSummaryJob>(
    "daily-email-summary",
    job => job.ExecuteAsync(),
    "0 8 * * *"); // 08:00 UTC daily
```

**Step 3** — For on-demand jobs (e.g., triggered after a successful order placement):
```csharp
BackgroundJob.Enqueue<OrderConfirmationEmailJob>(job => job.SendAsync(orderId));
```

No changes to DI or infrastructure are needed — Hangfire resolves dependencies from the DI container automatically.

---

## 8. Verification Steps

1. Start the application
2. Check SQL Server: confirm the `[HangFire]` schema and its 7 tables exist in `MoriiCoffeeDb`
3. Open `http://localhost:{port}/hangfire`
4. **Recurring Jobs** tab: verify `order-auto-complete` is listed with CRON `0 2 * * *` and a correct next run time
5. Click **Trigger now** on the dashboard to manually test the job logic
6. **Succeeded** tab: after execution, verify the job record shows the correct count of processed orders from the structured log output

---

## 9. Production Notes

- The dashboard currently allows localhost access only. Before production deployment, implement a proper `IDashboardAuthorizationFilter` or protect the `/hangfire` route at the reverse proxy level (nginx/Caddy)
- `DisableGlobalLocks = true` is configured in `SqlServerStorageOptions` to avoid lock contention if the application is scaled out to multiple instances
