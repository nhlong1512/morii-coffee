# Tóm tắt thay đổi — Migrate Background Job sang Hangfire (VN)

**Feature:** Background Job — OrderAutoComplete  
**Branch:** `009-cart-payment`  
**Ngày:** 2026-05-02  

---

## 1. Mục đích thay đổi

Phase 6 ban đầu implement `OrderAutoCompleteJob` dưới dạng .NET `BackgroundService` — đủ dùng cho 1 job đơn lẻ. Tuy nhiên, dự án sẽ cần thêm nhiều background job trong tương lai (ví dụ: gửi email xác nhận đơn hàng, email tóm tắt hàng ngày). Vì vậy, quyết định migrate sang **Hangfire** ngay ở giai đoạn này để:

- Có **retry tự động** khi job thất bại (quan trọng với email job)
- Có **dashboard** để monitor và quản lý các job
- Thêm job mới nhanh hơn, không cần viết lại vòng lặp scheduling thủ công
- Lưu lịch sử chạy job để audit trail

**Lý do chọn SQL Server (MoriiCoffeeDb) làm Hangfire storage:**  
Hangfire cần persistence store để lưu trạng thái job. Dự án ở scale nhỏ (vài chục đơn/ngày), nên dùng chung database hiện có là đủ — tránh thêm infrastructure. Hangfire tạo schema riêng `[HangFire]` không đụng chạm đến các bảng của ứng dụng.

**Redis không được chọn** vì Redis là in-memory: nếu restart mà không có persistence config, toàn bộ lịch sử job bị mất — không phù hợp với order/email jobs cần audit trail.

---

## 2. Files được tạo mới

### `source/MoriiCoffee.Infrastructure/Configurations/HangfireConfiguration.cs`

Extension method `ConfigureHangfire` đăng ký Hangfire vào DI container:

```csharp
services.AddHangfire(config => config
    .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
    .UseSimpleAssemblyNameTypeSerializer()
    .UseRecommendedSerializerSettings()
    .UseSqlServerStorage(connectionString, new SqlServerStorageOptions { ... }));

services.AddHangfireServer();
```

- `UseSqlServerStorage` — đọc `DefaultConnectionString` từ `appsettings.json`, tự tạo schema `[HangFire]` lần đầu khởi động
- `AddHangfireServer` — khởi động Hangfire worker process bên trong ứng dụng, lắng nghe và chạy các job được enqueue

---

## 3. Files được sửa đổi

### `source/MoriiCoffee.Infrastructure/BackgroundJobs/OrderAutoCompleteJob.cs`

**Trước (BackgroundService):**
- Kế thừa `BackgroundService`, override `ExecuteAsync`
- Có vòng lặp `while (!stoppingToken.IsCancellationRequested)` để tự tính `nextRun`
- Inject `IServiceScopeFactory` vì `BackgroundService` là singleton còn `IUnitOfWork` là scoped
- Tự tính delay: `todayRun = now.Date.AddHours(hour)` → `Task.Delay(nextRun - now)`

**Sau (Hangfire job):**
- Class thuần, **không kế thừa gì**
- Inject `IUnitOfWork` trực tiếp — Hangfire tự tạo DI scope mới cho mỗi lần chạy
- Không còn vòng lặp, không còn `IServiceScopeFactory`, không còn tính `nextRun` thủ công
- Logic nghiệp vụ bên trong giữ nguyên hoàn toàn:
  - Query đơn hàng `IN_DELIVERY` có `CreatedAt <= cutoffDate`
  - Gọi `order.MarkDelivered()` trên aggregate
  - `CommitAsync()` một lần cho toàn bộ batch

### `source/MoriiCoffee.Infrastructure/DependencyInjection.cs`

- **Xóa:** `services.AddHostedService<OrderAutoCompleteJob>()` — không còn dùng hosted service
- **Xóa:** `using MoriiCoffee.Infrastructure.BackgroundJobs` — namespace không còn cần thiết ở đây
- **Thêm:** `services.ConfigureHangfire(configuration)` — gọi extension method mới

### `source/MoriiCoffee.Presentation/Extensions/ApplicationExtensions.cs`

Thêm hai block vào pipeline (đặt sau `MapControllers`):

**Block 1 — Hangfire Dashboard:**
```csharp
app.UseHangfireDashboard("/hangfire", new DashboardOptions
{
    Authorization = [new LocalRequestsOnlyAuthorizationFilter()]
});
```
Dashboard chỉ accessible từ localhost. Trong production, nên bảo vệ thêm bằng reverse proxy hoặc VPN.

**Block 2 — Đăng ký Recurring Job:**
```csharp
var recurringJobs = app.Services.GetRequiredService<IRecurringJobManager>();
var orderSettings = app.Services.GetRequiredService<OrderSettings>();
recurringJobs.AddOrUpdate<OrderAutoCompleteJob>(
    "order-auto-complete",
    job => job.ExecuteAsync(CancellationToken.None),
    $"0 {orderSettings.AutoCompleteJobRunHour} * * *");
```
- Job ID `"order-auto-complete"` — unique identifier trong Hangfire dashboard
- CRON `"0 2 * * *"` (với `AutoCompleteJobRunHour = 2`) — chạy lúc 02:00 UTC mỗi ngày
- Nếu app restart, `AddOrUpdate` sẽ cập nhật lịch hiện có thay vì tạo duplicate

---

## 4. Packages được thêm

| Package | Version | Project |
|---|---|---|
| `Hangfire.Core` | 1.8.23 | Infrastructure |
| `Hangfire.SqlServer` | 1.8.23 | Infrastructure |
| `Hangfire.AspNetCore` | 1.8.23 | Infrastructure + Presentation |

`Hangfire.AspNetCore` cần có ở Infrastructure vì extension methods `AddHangfire` và `AddHangfireServer` được defined trong package đó.

---

## 5. Thay đổi Database

**Không cần EF Core migration.** Hangfire tự quản lý schema của nó.

Lần đầu ứng dụng khởi động, Hangfire tự tạo 7 bảng trong schema `[HangFire]`:

| Bảng | Mục đích |
|---|---|
| `[HangFire].[Job]` | Lưu từng job instance (type, args, state hiện tại) |
| `[HangFire].[JobParameter]` | Parameters của từng job |
| `[HangFire].[JobQueue]` | Queue các job đang chờ chạy |
| `[HangFire].[State]` | Lịch sử state transitions (Enqueued → Processing → Succeeded/Failed) |
| `[HangFire].[Counter]` | Thống kê tổng hợp (succeeded, failed count) |
| `[HangFire].[Hash]` | Config recurring jobs (CRON, last run time) |
| `[HangFire].[Set]` | Internal data structures |

---

## 6. Thay đổi API

Không có endpoint API mới. Dashboard Hangfire là web UI riêng:

- **URL:** `http://localhost:{port}/hangfire`
- **Access:** Chỉ từ localhost (`LocalRequestsOnlyAuthorizationFilter`)

---

## 7. Thêm job mới trong tương lai

Để thêm một background job mới (ví dụ: gửi email hàng ngày):

**Bước 1** — Tạo class job trong `MoriiCoffee.Infrastructure/BackgroundJobs/`:
```csharp
public class DailyEmailSummaryJob
{
    public DailyEmailSummaryJob(IEmailService emailService) { ... }
    public async Task ExecuteAsync() { ... }
}
```

**Bước 2** — Đăng ký recurring job trong `ApplicationExtensions.cs`:
```csharp
recurringJobs.AddOrUpdate<DailyEmailSummaryJob>(
    "daily-email-summary",
    job => job.ExecuteAsync(),
    "0 8 * * *"); // 08:00 UTC mỗi ngày
```

**Bước 3** — Để enqueue job on-demand (ví dụ: sau khi đặt hàng thành công):
```csharp
BackgroundJob.Enqueue<OrderConfirmationEmailJob>(job => job.SendAsync(orderId));
```

Không cần thay đổi DI hay infrastructure — Hangfire resolve dependencies tự động từ DI container.

---

## 8. Cách verify

1. Chạy ứng dụng
2. Kiểm tra SQL Server: schema `[HangFire]` và 7 bảng đã được tạo trong `MoriiCoffeeDb`
3. Truy cập `http://localhost:{port}/hangfire`
4. Tab **Recurring Jobs**: thấy `order-auto-complete` với CRON `0 2 * * *` và `Next run` đúng
5. Trigger thủ công bằng nút **Trigger now** trên dashboard để test logic
6. Tab **Succeeded**: sau khi chạy xong, thấy job với số lượng order đã xử lý trong logs

---

## 9. Lưu ý production

- Dashboard hiện chỉ cho phép localhost. Trước khi deploy production, implement `IDashboardAuthorizationFilter` hoặc bảo vệ route `/hangfire` bằng nginx/reverse proxy
- Nếu muốn nhiều server chạy song song (scale-out), Hangfire `DisableGlobalLocks = true` đã được cấu hình để tránh lock contention
