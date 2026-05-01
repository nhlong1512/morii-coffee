# Kế Hoạch Implement Backend — Cart & Order Management

**Feature:** `cart-order-management`  
**Branch:** `009-cart-payment`  
**Ngày cập nhật:** 2026-05-02  
**Ngôn ngữ:** C# / .NET 10.0 — Clean Architecture

---

## Mục Lục

1. [Hiện trạng backend](#1-hiện-trạng-backend)
2. [NuGet Packages cần thêm](#2-nuget-packages-cần-thêm)
3. [Domain Layer](#3-domain-layer)
4. [Application Layer — CQRS](#4-application-layer--cqrs)
5. [Infrastructure — Redis Cart Service](#5-infrastructure--redis-cart-service)
6. [Infrastructure — Order ID Generator](#6-infrastructure--order-id-generator)
7. [Infrastructure — Auto-Complete Background Job](#7-infrastructure--auto-complete-background-job)
8. [Presentation Layer — Endpoints mới](#8-presentation-layer--endpoints-mới)
9. [Database — Migrations mới](#9-database--migrations-mới)
10. [Thứ tự implement đề xuất](#10-thứ-tự-implement-đề-xuất)

---

## 1. Hiện trạng backend

### Cấu trúc dự án (Clean Architecture — 8 projects)

| Project | Vai trò |
|---|---|
| `MoriiCoffee.Domain` | Entities, Aggregates, Interfaces |
| `MoriiCoffee.Domain.Shared` | Enums, DTOs, Constants, Settings |
| `MoriiCoffee.Application` | Commands, Queries, Handlers, Validators, Mapper Profiles |
| `MoriiCoffee.Infrastructure` | DI setup, Service implementations, Configurations |
| `MoriiCoffee.Infrastructure.Persistence` | EF Core DbContext, Repositories, Migrations |
| `MoriiCoffee.Presentation` | ASP.NET Core Controllers, Middlewares |
| `MoriiCoffee.Application.Tests` | Unit tests cho Application layer |
| `MoriiCoffee.Domain.Tests` | Unit tests cho Domain layer |

### Những gì đã có

| Layer | Đã implement |
|---|---|
| Domain | User, Product, ProductVariant, ProductImage, Category, Banner |
| Application | Auth, Product CRUD, Category CRUD, Banner CRUD, User profile |
| Infrastructure | JWT, Google OAuth, Email (Brevo), File Storage (S3/MinIO) |
| Presentation | AuthController, ProductsController, CategoriesController, BannersController, UsersController, FilesController |
| Database | 8 tables: Users, Roles, Categories, Banners, Products, ProductVariants, ProductImages, ProductCategories |

### Những gì còn thiếu cho Cart & Order

| Layer | Cần thêm |
|---|---|
| Domain | Order aggregate, Enums mới |
| Application | Cart handlers, Order handlers, Delivery profile handlers |
| Infrastructure | Redis cart state + cache foundation, Background job, Order ID generator |
| Presentation | CartController, OrdersController, mở rộng UsersController |
| Database | Bảng orders, order_items, user_delivery_profiles |

---

## 2. Setup Redis từ đầu

### Bước 2.1 — Thêm Redis vào Docker Compose (local dev)

Mở file `deploy/docker-compose.development.yml` (hoặc file compose hiện tại), thêm service Redis:

```yaml
services:
  # ... các service hiện có ...

  redis:
    image: redis:7-alpine
    container_name: morii-redis
    ports:
      - "6379:6379"
    volumes:
      - redis_data:/data
    command: redis-server --appendonly yes   # bật persistence
    restart: unless-stopped
    networks:
      - morii-network

volumes:
  redis_data:
```

Sau đó chạy lại:

```bash
cd deploy && bash run-docker-development.sh
# hoặc docker compose up -d redis
```

Kiểm tra Redis đã chạy:

```bash
docker exec -it morii-redis redis-cli ping
# Expected: PONG
```

### Bước 2.2 — Thêm NuGet packages

```bash
# Project: MoriiCoffee.Infrastructure
dotnet add source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj \
  package StackExchange.Redis

dotnet add source/MoriiCoffee.Infrastructure/MoriiCoffee.Infrastructure.csproj \
  package Microsoft.Extensions.Caching.StackExchangeRedis
```

Hoặc thêm trực tiếp vào `.csproj`:

```xml
<!-- MoriiCoffee.Infrastructure.csproj -->
<PackageReference Include="StackExchange.Redis" Version="2.8.*" />
<PackageReference Include="Microsoft.Extensions.Caching.StackExchangeRedis" Version="10.0.*" />
```

### Bước 2.3 — Thêm cấu hình vào appsettings

Hiện codebase đã có `ConfigureRedis(configuration)` và đang đọc connection string bằng:

```csharp
configuration.GetConnectionString("CachingConnectionString")
```

Vì vậy nên giữ cấu hình Redis theo `ConnectionStrings` để nhất quán với code hiện tại.

**`appsettings.json`** (template, không có giá trị thật):

```json
"ConnectionStrings": {
  "CachingConnectionString": ""
}
```

**`appsettings.Development.json`** (local dev):

```json
"ConnectionStrings": {
  "CachingConnectionString": "localhost:6379"
}
```

**`appsettings.Production.json`** (production — điền sau khi có Redis server):

```json
"ConnectionStrings": {
  "CachingConnectionString": "your-redis-host:6379,password=xxx,ssl=True"
}
```

### Bước 2.4 — Settings cho Redis

Ở giai đoạn đầu chưa cần `RedisSettings` riêng nếu chỉ dùng:
- `IDistributedCache`
- `ICacheService`
- `CachedKeyConstants`
- `CacheTtlConstants`

Chỉ tạo `RedisSettings` khi thật sự cần thêm nhiều tham số runtime như:
- `InstanceName`
- Redis database index
- retry / timeout policy

TTL không nên nằm trong settings ở thời điểm này. TTL hiện nên thống nhất ở constants:

```csharp
// source/MoriiCoffee.Domain.Shared/Constants/CacheTtlConstants.cs
public static class CacheTtlConstants
{
    public static readonly TimeSpan Default = TimeSpan.FromMinutes(2);
    public static readonly TimeSpan Cart = TimeSpan.FromHours(24);
}
```

### Bước 2.5 — Đăng ký Redis trong DI

**Đường dẫn chính:** `/source/MoriiCoffee.Infrastructure/Configurations/CachingConfiguration.cs`

Giữ registration theo extension `ConfigureRedis(configuration)` như codebase hiện tại:

```csharp
services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = configuration.GetConnectionString("CachingConnectionString");
});
```

> **Lưu ý:** `AddStackExchangeRedisCache` đăng ký `IDistributedCache`.  
> `IConnectionMultiplexer` chưa cần đăng ký ngay. Chỉ thêm khi thật sự dùng Redis trực tiếp cho:
> - atomic counter tạo order sequence
> - distributed lock
> - pub/sub
> - Lua script / pipeline

### Bước 2.6 — Kiểm tra kết nối

Thêm health check để verify Redis hoạt động khi startup:

```csharp
// Trong DependencyInjection.cs hoặc Program.cs
services.AddHealthChecks()
    .AddRedis(configuration.GetConnectionString("CachingConnectionString")!, name: "redis");
```

Sau đó map endpoint (trong `Program.cs`):

```csharp
app.MapHealthChecks("/health");
```

Gọi `GET /health` → kết quả có `redis: Healthy` là xong.

---

## 2b. NuGet Packages khác cần thêm

### Background Job

```xml
<!-- Không cần thêm package — dùng IHostedService built-in -->
<!-- Phù hợp với quy mô nhỏ, chạy 1 lần/ngày -->
```

---

## 3. Domain Layer

### 3.1 Enums mới

**Đường dẫn:** `/source/MoriiCoffee.Domain.Shared/Enums/Order/`

```csharp
// EOrderStatus.cs
public enum EOrderStatus
{
    PENDING         = 1,  // Đơn hàng đã đặt
    CONFIRMED       = 2,  // Đã xác nhận
    READY_TO_PICKUP = 3,  // Chờ lấy hàng
    IN_DELIVERY     = 4,  // Đang giao
    DELIVERED       = 5,  // Đã giao thành công
    REVIEWED        = 6,  // Đã đánh giá
    CANCELLED       = 7   // Đã huỷ
}

// EPaymentMethod.cs
public enum EPaymentMethod
{
    COD    = 1,  // Thanh toán khi nhận hàng
    MOMO   = 2,  // Ví MoMo (tích hợp sau)
    PAYPAL = 3   // PayPal (tích hợp sau)
}
```

### 3.2 Order Aggregate

**Đường dẫn:** `/source/MoriiCoffee.Domain/Aggregates/OrderAggregate/`

```
OrderAggregate/
  Order.cs              ← AggregateRoot
  Entities/
    OrderItem.cs        ← Entity (không phải AggregateRoot)
  ValueObjects/
    DeliveryInfo.cs     ← ValueObject (immutable)
```

**Order.cs** — cấu trúc:

```csharp
public class Order : AggregateRoot   // kế thừa EntityBase, IDateTracking, ISoftDeletable
{
    public Guid Id { get; private set; }             // internal technical id
    public string OrderNumber { get; private set; }  // MRC-YYYYMMDD-NNN, hiển thị cho FE
    public Guid UserId { get; private set; }
    public DeliveryInfo DeliveryInfo { get; private set; }
    public string? Notes { get; private set; }
    public EPaymentMethod PaymentMethod { get; private set; }
    public decimal Subtotal { get; private set; }
    public decimal Tax { get; private set; }
    public decimal Shipping { get; private set; }
    public decimal Discount { get; private set; }
    public decimal Total { get; private set; }
    public EOrderStatus OrderStatus { get; private set; }
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    // Factory method thay vì constructor public
    public static Order Create(...) { }

    // Domain methods
    public void Confirm() { }
    public void MarkReadyToPickup() { }
    public void MarkInDelivery() { }
    public void MarkDelivered() { }
    public void Cancel() { }
    public void UpdateStatus(EOrderStatus newStatus) { }
}
```

**OrderItem.cs** — snapshot tại thời điểm đặt hàng:

```csharp
public class OrderItem : EntityBase
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; }    // snapshot
    public Guid? VariantId { get; private set; }
    public string? VariantLabel { get; private set; }  // snapshot (e.g. "Size M")
    public decimal UnitPrice { get; private set; }     // snapshot
    public int Quantity { get; private set; }
    public decimal LineTotal { get; private set; }     // UnitPrice * Quantity
}
```

**DeliveryInfo.cs** — ValueObject bất biến:

```csharp
public record DeliveryInfo(
    string FullName,
    string PhoneNumber,
    string Address
);
```

> **Ghi chú về identity:** `Order.Id` là technical id để backend/DB dùng nội bộ.  
> `OrderNumber` là business/display id để show ra FE, email, lịch sử đơn hàng, và hỗ trợ customer support tra cứu.
>
> **Lý do snapshot:** Sản phẩm có thể bị sửa giá hoặc xoá sau khi đặt hàng. Snapshot đảm bảo lịch sử đơn hàng luôn chính xác.

---

## 4. Application Layer — CQRS

### 4.1 Cart

**Đường dẫn:** `/source/MoriiCoffee.Application/Commands/Cart/` và `.../Queries/Cart/`

```
Commands/Cart/
  AddItemToCart/
    AddItemToCartCommand.cs
    AddItemToCartCommandHandler.cs
    AddItemToCartCommandValidator.cs

  RemoveItemFromCart/
    RemoveItemFromCartCommand.cs
    RemoveItemFromCartCommandHandler.cs

  UpdateCartItemQuantity/
    UpdateCartItemQuantityCommand.cs
    UpdateCartItemQuantityCommandHandler.cs
    UpdateCartItemQuantityCommandValidator.cs

  ClearCart/
    ClearCartCommand.cs
    ClearCartCommandHandler.cs

  MergeGuestCart/
    MergeGuestCartCommand.cs       ← chứa List<GuestCartItemDto>
    MergeGuestCartCommandHandler.cs

Queries/Cart/
  GetCart/
    GetCartQuery.cs
    GetCartQueryHandler.cs         ← gọi ICartService.GetCartAsync(userId)
```

**Merge rule khi login:**

```
Redis cart:          [Cà phê sữa x1, Bánh mì x1]
localStorage cart:   [Cà phê sữa x2, Trà đào x1]
                          ↓ merge
Kết quả:             [Cà phê sữa x3, Bánh mì x1, Trà đào x1]
```
→ Cùng `productId + variantId` → **cộng dồn quantity** (theo convention Shopee, GrabFood).

### 4.2 Order

**Đường dẫn:** `/source/MoriiCoffee.Application/Commands/Order/` và `.../Queries/Order/`

```
Commands/Order/
  PlaceOrder/
    PlaceOrderCommand.cs           ← deliveryInfo, paymentMethod, notes, saveDeliveryInfo
    PlaceOrderCommandHandler.cs    ← validate stock → snapshot giá → tạo Order → clear cart
    PlaceOrderCommandValidator.cs

  CancelOrder/
    CancelOrderCommand.cs
    CancelOrderCommandHandler.cs   ← chỉ cho phép khi status = PENDING, chỉ owner

  UpdateOrderStatus/
    UpdateOrderStatusCommand.cs    ← orderId, newStatus
    UpdateOrderStatusCommandHandler.cs  ← ADMIN only

Queries/Order/
  GetMyOrders/
    GetMyOrdersQuery.cs            ← page, pageSize, statusFilter?
    GetMyOrdersQueryHandler.cs

  GetOrderById/
    GetOrderByIdQuery.cs
    GetOrderByIdQueryHandler.cs    ← owner hoặc admin mới được xem

  GetAllOrders/                    ← ADMIN only
    GetAllOrdersQuery.cs           ← page, pageSize, status?, dateFrom?, dateTo?, customerName?
    GetAllOrdersQueryHandler.cs
```

**PlaceOrderCommandHandler — logic quan trọng:**

```
1. Load cart từ Redis theo userId (`CachedKeyConstants.CartByUser(userId)`)
2. Validate cart không rỗng
3. Với mỗi item trong cart:
   a. Load Product + Variant từ DB
   b. Kiểm tra sản phẩm còn available không → nếu không, throw lỗi chỉ rõ sản phẩm nào
   c. Snapshot: productName, variantLabel, unitPrice
4. Tính toán server-side: subtotal, tax, shipping, discount, total
5. Generate order number qua IOrderIdGenerator
6. Tạo Order entity + OrderItems
7. Lưu vào DB
8. Nếu saveDeliveryInfo = true → upsert UserDeliveryProfile
9. Commit transaction thành công
10. Clear Redis cart
11. Return orderId + orderNumber
```

> **Quan trọng:** Chỉ các thao tác DB nên nằm trong transaction.  
> `Clear Redis cart` nên chạy **sau khi commit DB thành công** để tránh trường hợp DB rollback nhưng cart đã bị xoá.

### 4.3 Delivery Profile

**Đường dẫn:** `/source/MoriiCoffee.Application/Commands/User/` và `.../Queries/User/`

```
Commands/User/
  SaveDeliveryProfile/
    SaveDeliveryProfileCommand.cs      ← fullName, phoneNumber, address
    SaveDeliveryProfileCommandHandler.cs   ← upsert theo userId

Queries/User/
  GetMyDeliveryProfile/
    GetMyDeliveryProfileQuery.cs
    GetMyDeliveryProfileQueryHandler.cs
```

---

## 5. Infrastructure — Redis Cart Service

**Đường dẫn:** `/source/MoriiCoffee.Infrastructure/Services/Cart/`

```
ICartService.cs
RedisCartService.cs
```

**ICartService.cs:**

```csharp
public interface ICartService
{
    Task<CartDto> GetCartAsync(Guid userId);
    Task AddItemAsync(Guid userId, CartItemDto item);
    Task RemoveItemAsync(Guid userId, Guid productId, Guid? variantId);
    Task UpdateQuantityAsync(Guid userId, Guid productId, Guid? variantId, int quantity);
    Task ClearCartAsync(Guid userId);
    Task MergeAsync(Guid userId, List<CartItemDto> guestItems);
}
```

**Redis key & TTL:**

```
Key:  CachedKeyConstants.CartByUser(userId)   // cart:{userId}
Type: String (JSON serialized)
TTL:  CacheTtlConstants.Cart = 24 giờ
```

> Với flow hiện tại, cart là state ngắn hạn trong Redis.  
> Không cần generic cache TTL theo nhiều mức; chỉ cần:
> - `CacheTtlConstants.Default = 2 phút` cho cache read-model thông thường
> - `CacheTtlConstants.Cart = 24 giờ` cho cart

**Cart JSON structure trong Redis:**

```json
{
  "items": [
    {
      "productId": "guid",
      "variantId": "guid | null",
      "variantLabel": "Size M",
      "productName": "Cà phê sữa",
      "unitPrice": 45000,
      "quantity": 2,
      "imageUrl": "https://...",
      "addedAt": "2026-04-27T..."
    }
  ],
  "updatedAt": "2026-04-27T..."
}
```

**Đăng ký DI** trong `DependencyInjection.cs`:

```csharp
services.AddScoped<ICartService, RedisCartService>();
```

**Triển khai khuyến nghị cho `RedisCartService`:**

- Dùng `ICacheService` nếu muốn tận dụng serializer/logging hiện đã có trong Infrastructure
- Hoặc dùng `IDistributedCache` trực tiếp nếu service cần tối giản và chỉ lưu 1 payload cart
- Không cần dùng `ICachedRepositoryBase<T>` cho cart, vì cart là state object trong Redis chứ không phải DB entity cache

**appsettings.json:**

```json
"ConnectionStrings": {
  "CachingConnectionString": "localhost:6379"
}
```

---

## 6. Infrastructure — Order ID Generator

**Đường dẫn:** `/source/MoriiCoffee.Infrastructure/Services/Order/`

```
IOrderIdGenerator.cs
OrderIdGenerator.cs
```

**Logic generate ID:**

```csharp
// Format: MRC-YYYYMMDD-NNN
// Ví dụ: MRC-20260427-001

public async Task<string> GenerateAsync()
{
    var today = DateTime.UtcNow.ToString("yyyyMMdd");
    var count = await _dbContext.Orders
        .CountAsync(o => o.OrderNumber.StartsWith($"MRC-{today}-"));
    var seq = (count + 1).ToString("D3");  // zero-pad: 001, 002, ...
    return $"MRC-{today}-{seq}";
}
```

> ⚠️ **Race condition:** Ở quy mô nhỏ, query DB có thể dùng tạm. Khi traffic tăng (nhiều đơn cùng lúc), cần dùng atomic counter trong Redis (`INCR MRC-order-seq:{date}`) hoặc một cơ chế sequence khác để tránh duplicate ID.
>
> **Lưu ý thêm:** Dù `Order.Id` đã dùng `Guid`, `Order` vẫn nên có `IOrderRepository` riêng thay vì ép mọi truy vấn admin/customer vào generic repository. Aggregate này có nhu cầu query/filter riêng theo `OrderNumber`, `OrderStatus`, `CreatedAt`, `UserId`.

---

## 7. Infrastructure — Auto-Complete Background Job

> ✅ **Đã implement bằng Hangfire** (thay vì .NET BackgroundService như plan gốc)

**Đường dẫn:** `/source/MoriiCoffee.Infrastructure/BackgroundJobs/`

```
OrderAutoCompleteJob.cs    ← Hangfire recurring job (plain class, không kế thừa gì)
```

**Logic:**

```csharp
// Mỗi ngày lúc 2:00 UTC, Hangfire tự trigger
// Tìm tất cả đơn IN_DELIVERY có CreatedAt <= (now - 3 ngày)
// → order.MarkDelivered() → CommitAsync() một lần cho toàn batch

[DisableConcurrentExecution(timeoutInSeconds: 10 * 60)]
public async Task ExecuteAsync(CancellationToken ct = default)
{
    var cutoffDate = DateTime.UtcNow.AddDays(-3);
    var staleOrders = await _unitOfWork.Orders
        .FindByCondition(o => !o.IsDeleted
            && o.OrderStatus == EOrderStatus.IN_DELIVERY
            && o.CreatedAt <= cutoffDate, trackChanges: true)
        .ToListAsync(ct);

    foreach (var order in staleOrders) order.MarkDelivered();
    await _unitOfWork.CommitAsync();
}
```

**Cấu hình trong `appsettings.json`:**

```json
"OrderSettings": {
  "AutoCompleteJobRunHour": 2
}
```

**Đăng ký DI và recurring job:**

```csharp
// Infrastructure/DependencyInjection.cs
services.ConfigureHangfire(configuration);  // AddHangfire + AddHangfireServer + SqlServer storage

// Presentation/Extensions/HangfireJobsExtensions.cs
recurringJobs.AddOrUpdate<OrderAutoCompleteJob>(
    "order-auto-complete",
    job => job.ExecuteAsync(CancellationToken.None),
    $"0 {orderSettings.AutoCompleteJobRunHour} * * *");

// Presentation/Extensions/ApplicationExtensions.cs
app.UseHangfireDashboard();  // /hangfire — localhost only
```

**Lý do dùng Hangfire thay BackgroundService:**
- Có retry tự động khi job fail
- Dashboard tại `/hangfire` để monitor và trigger thủ công
- Dễ thêm job mới (email, cleanup...) mà không cần viết lại vòng lặp scheduling
- Lưu lịch sử chạy job để audit trail

---

## 8. Presentation Layer — Endpoints mới

### CartController

**Route:** `api/v1/cart`  
**Auth:** Bắt buộc đăng nhập (tất cả endpoints)

```
GET    /api/v1/cart                → GetCart
POST   /api/v1/cart/items          → AddItemToCart
PUT    /api/v1/cart/items          → UpdateCartItemQuantity
DELETE /api/v1/cart/items          → RemoveItemFromCart
DELETE /api/v1/cart                → ClearCart
POST   /api/v1/cart/merge          → MergeGuestCart (gọi ngay sau khi login thành công)
```

### OrdersController

**Route:** `api/v1/orders`

```
POST   /api/v1/orders                        → PlaceOrder            [Authorize]
GET    /api/v1/orders                        → GetMyOrders           [Authorize]
GET    /api/v1/orders/{orderId}              → GetOrderById          [Authorize] (owner or admin, `orderId` là Guid)
PATCH  /api/v1/orders/{orderId}/cancel       → CancelOrder           [Authorize] (owner, PENDING only, `orderId` là Guid)

GET    /api/v1/orders/admin                  → GetAllOrders          [Authorize(ADMIN)]
PATCH  /api/v1/orders/{orderId}/status       → UpdateOrderStatus     [Authorize(ADMIN), `orderId` là Guid)
```

### UsersController (mở rộng)

```
GET    /api/v1/users/me/delivery-profile     → GetMyDeliveryProfile  [Authorize]
PUT    /api/v1/users/me/delivery-profile     → SaveDeliveryProfile   [Authorize]
```

### Response format (nhất quán với các controller hiện tại)

```csharp
// Tạo đơn thành công
201 Created
{
  "id": "guid",
  "orderNumber": "MRC-20260427-001"
}

// Lấy danh sách đơn
200 OK
{
  "items": [ OrderSummaryDto ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10
}

// Lỗi sản phẩm hết hàng
422 Unprocessable Entity
{
  "message": "Một số sản phẩm không còn khả dụng",
  "unavailableItems": ["Cà phê sữa - Size M"]
}
```

---

## 9. Database — Migrations mới

**3 bảng cần thêm:**

### orders

```sql
CREATE TABLE orders (
    id               UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    order_number     NVARCHAR(20)    NOT NULL UNIQUE,        -- MRC-YYYYMMDD-NNN, hiển thị cho FE
    user_id          UNIQUEIDENTIFIER NOT NULL,
    full_name        NVARCHAR(100)   NOT NULL,
    phone_number     NVARCHAR(15)    NOT NULL,
    address          NVARCHAR(500)   NOT NULL,
    notes            NVARCHAR(500)   NULL,
    payment_method   TINYINT         NOT NULL,               -- EPaymentMethod enum
    subtotal         DECIMAL(18, 2)  NOT NULL,
    tax              DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    shipping         DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    discount         DECIMAL(18, 2)  NOT NULL DEFAULT 0,
    total            DECIMAL(18, 2)  NOT NULL,
    order_status     TINYINT         NOT NULL DEFAULT 1,     -- EOrderStatus.PENDING
    is_deleted       BIT             NOT NULL DEFAULT 0,
    created_at       DATETIME2       NOT NULL,
    updated_at       DATETIME2       NOT NULL,
    created_by       NVARCHAR(256)   NULL,
    updated_by       NVARCHAR(256)   NULL,

    CONSTRAINT FK_orders_users FOREIGN KEY (user_id) REFERENCES AspNetUsers(Id)
);

CREATE INDEX IX_orders_user_id ON orders(user_id);
CREATE INDEX IX_orders_order_number ON orders(order_number);
CREATE INDEX IX_orders_order_status ON orders(order_status);
CREATE INDEX IX_orders_created_at ON orders(created_at DESC);
```

### order_items

```sql
CREATE TABLE order_items (
    id               UNIQUEIDENTIFIER NOT NULL PRIMARY KEY DEFAULT NEWID(),
    order_id         UNIQUEIDENTIFIER NOT NULL,
    product_id       UNIQUEIDENTIFIER NOT NULL,
    product_name     NVARCHAR(200)   NOT NULL,   -- snapshot tại thời điểm đặt
    variant_id       UNIQUEIDENTIFIER NULL,
    variant_label    NVARCHAR(50)    NULL,        -- snapshot (e.g. "Size M")
    unit_price       DECIMAL(18, 2)  NOT NULL,   -- snapshot
    quantity         INT             NOT NULL,
    line_total       DECIMAL(18, 2)  NOT NULL,   -- unit_price * quantity

    CONSTRAINT FK_order_items_orders FOREIGN KEY (order_id) REFERENCES orders(id)
);
```

### user_delivery_profiles

```sql
CREATE TABLE user_delivery_profiles (
    user_id          UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
    full_name        NVARCHAR(100)   NOT NULL,
    phone_number     NVARCHAR(15)    NOT NULL,
    address          NVARCHAR(500)   NOT NULL,
    updated_at       DATETIME2       NOT NULL,

    CONSTRAINT FK_delivery_profiles_users FOREIGN KEY (user_id) REFERENCES AspNetUsers(Id)
);
```

**Tạo migration với EF Core:**

```bash
dotnet ef migrations add AddOrderAggregate \
  --project source/MoriiCoffee.Infrastructure.Persistence \
  --startup-project source/MoriiCoffee.Presentation
```

---

## 10. Thứ tự implement đề xuất

```
Phase 1 — Redis Setup (không thể skip, làm trước tất cả)
  ☑ 1. Thêm Redis service vào docker-compose.development.yml
  ☑ 2. Cài NuGet: StackExchange.Redis + Microsoft.Extensions.Caching.StackExchangeRedis
  ☑ 3. Thêm `ConnectionStrings:CachingConnectionString` vào appsettings.json + appsettings.Development.json
  ☑ 4. Giữ `ConfigureRedis(configuration)` dùng `AddStackExchangeRedisCache`
  ☑ 5. Chưa thêm `IConnectionMultiplexer` cho đến khi thực sự cần atomic counter / lock
  ☑ 6. Thêm health check Redis, verify GET /health trả về Healthy

Phase 2 — Domain & Database Foundation
  ☑ 7.  Thêm EOrderStatus, EPaymentMethod enums vào Domain.Shared/Enums/Order/
  ☑ 8.  Tạo Order aggregate + OrderItem entity + DeliveryInfo value object
  ☑ 9.  Thêm cấu hình EF Core cho Order, OrderItem (Fluent API)
  ☑ 10. Tạo EF migration AddOrderAggregate (3 bảng: orders, order_items, user_delivery_profiles)
  ☑ 11. Tạo IOrderRepository + implementation + đăng ký DI

Phase 3 — Cart APIs
  ☑ 12. Tạo ICartService + RedisCartService (dùng IDistributedCache)
  ☑ 13. Implement Cart Commands/Queries + Handlers (AddItem, Remove, UpdateQty, Clear, Merge, GetCart)
  ☑ 13.1 Dùng `CachedKeyConstants.CartByUser(userId)` và `CacheTtlConstants.Cart`
  ☑ 14. Tạo CartController (6 endpoints)

Phase 4 — Order APIs
  ☑ 15. Tạo IOrderIdGenerator + OrderIdGenerator
  ☑ 16. Implement PlaceOrderCommandHandler (logic phức tạp nhất — xem section 4.2)
  ☑ 17. Implement CancelOrderCommandHandler
  ☑ 18. Implement GetMyOrders, GetOrderById Queries
  ☑ 19. Tạo OrdersController (6 endpoints)
  ☑ 20. Implement UpdateOrderStatus (admin) + GetAllOrders (admin)

Phase 5 — Delivery Profile
  ☑ 21. Implement SaveDeliveryProfile + GetMyDeliveryProfile
  ☑ 22. Mở rộng UsersController (2 endpoints)

Phase 6 — Background Job
  ☑ 23. Thêm OrderSettings vào appsettings.json (AutoCompleteJobRunHour)
  ☑ 24. Tạo OrderAutoCompleteJob (Hangfire recurring job — thay BackgroundService)
  ☑ 25. Đăng ký Hangfire (ConfigureHangfire, dashboard, RecurringJob)

Phase 7 — Hardening
  ☐ 26. Unit tests cho PlaceOrderCommandHandler (case: stock hết, giá thay đổi, cart rỗng)
  ☐ 27. Unit tests cho CancelOrderCommandHandler (case: không phải owner, không còn PENDING)
  ☐ 28. Integration test cho cart merge flow
```

---

## Tóm tắt nhanh

| Hạng mục | Số lượng | Chi tiết |
|---|---|---|
| Phases | 7 | Redis → Domain/DB → Cart → Order → Profile → Job → Tests |
| NuGet packages mới | 5 | StackExchange.Redis, Microsoft.Extensions.Caching.StackExchangeRedis, Hangfire.Core, Hangfire.SqlServer, Hangfire.AspNetCore |
| Docker services mới | 1 | redis:7-alpine |
| Config sections mới | 2 | ConnectionStrings (CachingConnectionString), OrderSettings |
| Enums mới | 2 | EOrderStatus, EPaymentMethod |
| Domain entities/VOs mới | 3 | Order, OrderItem, DeliveryInfo |
| Commands mới | 9 | Cart (5) + Order (3) + Profile (1) |
| Queries mới | 5 | Cart (1) + Order (3) + Profile (1) |
| Controllers mới | 2 | CartController, OrdersController |
| Endpoints mở rộng (Users) | 2 | delivery-profile GET/PUT |
| Infrastructure services mới | 3 | RedisCartService, OrderIdGenerator, OrderAutoCompleteJob (Hangfire) |
| DB migrations | 1 | 3 bảng: orders, order_items, user_delivery_profiles |
| Tổng tasks checklist | 28 | Phân bổ theo 7 phases |
