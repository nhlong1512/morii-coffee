# Infrastructure Services — MoriiCoffee

## Redis Caching

### Cache Service Interface (Application layer)
```csharp
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key);
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null);
    Task RemoveAsync(string key);
    Task RemoveByPrefixAsync(string prefix);
}
```

### Cache Key Constants (Domain.Shared)
```csharp
public static class CacheKeys
{
    public const string PRODUCTS = "products";
    public const string PRODUCT_BY_ID = "product:{0}";     // string.Format with id
    public const string CATEGORIES = "categories";
    public const string ORDERS = "orders";
    // ... extend as needed
}
```

### Cached Repository Pattern
Write operations in handlers must invalidate cache:
```csharp
// After create/update/delete in handler:
await _cacheService.RemoveAsync(CacheKeys.PRODUCTS);
await _cacheService.RemoveAsync(string.Format(CacheKeys.PRODUCT_BY_ID, product.Id));
```

### Docker service
```yaml
moriicoffee.cache:
  image: redis:7.0.15-alpine
  ports:
    - "6379:6379"
  networks:
    - moriicoffee
```

---

## MinIO File Storage

### File Service Interface (Application layer)
```csharp
public interface IFileService
{
    Task<string> UploadAsync(IFormFile file, string container);
    Task<Stream> GetAsync(string fileId, string container);
    Task DeleteAsync(string fileId, string container);
}
```

### File Container Constants (Domain.Shared)
```csharp
public static class FileContainers
{
    public const string USERS = "users";        // avatars
    public const string PRODUCTS = "products";  // product images
    public const string MENUS = "menus";        // menu images
}
```

### Upload in a command handler
```csharp
// File upload happens in Presentation layer (controller receives IFormFile)
// then passes the URL (already uploaded) as a string field on the command,
// OR the handler receives IFormFile and calls IFileService directly.

var imageUrl = await _fileService.UploadAsync(request.CoverImage, FileContainers.PRODUCTS);
product.SetCoverImage(imageUrl);
```

### Docker service
```yaml
moriicoffee.minio:
  image: minio/minio
  ports:
    - "9000:9000"
    - "9001:9001"
  environment:
    MINIO_ROOT_USER: minioadmin
    MINIO_ROOT_PASSWORD: minioadmin
  command: server /data --console-address ":9001"
  networks:
    - moriicoffee
```

**Gotcha:** When the API runs outside Docker (`dotnet run`), use `localhost:9000` in `appsettings.Development.json`, not the container name.

---

## Hangfire Background Jobs

Used for: email notifications, scheduled cleanup, retry logic.

### Setup (Infrastructure)
```csharp
// Configurations/HangfireConfiguration.cs
services.AddHangfire(config =>
    config.SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
          .UseSimpleAssemblyNameTypeSerializer()
          .UseRecommendedSerializerSettings()
          .UseMongoStorage(connectionString, new MongoStorageOptions { ... }));

services.AddHangfireServer();
```

### Enqueue a job (from a handler)
```csharp
_backgroundJobClient.Enqueue<IEmailService>(
    service => service.SendOrderConfirmationAsync(order.Id));
```

### Dashboard
```csharp
// In Program.cs
app.UseHangfireDashboard("/hangfire", new DashboardOptions {
    Authorization = [new HangfireAuthFilter()]  // secure in production!
});
```

### Docker service
```yaml
moriicoffee.hangfire:
  image: mongo:latest
  ports:
    - "27017:27017"
  networks:
    - moriicoffee
```

---

## SignalR Real-time Hubs

### Hub structure (Application layer)
```csharp
// Hubs/NotificationHub.cs
[Authorize]
public class NotificationHub : Hub
{
    public async Task Connect(string userId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, userId);
    }
}
```

### Send notification from a handler
```csharp
// Inject IHubContext<NotificationHub>
await _hubContext.Clients
    .Group(targetUserId.ToString())
    .SendAsync("ReceiveNotification", notificationDto);
```

### Register in Program.cs
```csharp
app.MapHub<NotificationHub>("/notification");
```

---

## JWT Authentication

### JwtOptions (Domain.Shared)
```csharp
public class JwtOptions
{
    public string Secret { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
    public int AccessTokenExpiryInMinutes { get; set; } = 60;
    public int RefreshTokenExpiryInDays { get; set; } = 7;
}
```

### Configuration (Infrastructure)
```csharp
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options => {
        options.TokenValidationParameters = new TokenValidationParameters {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Secret)),
            ClockSkew = TimeSpan.Zero   // tokens expire exactly at expiry time
        };
    });
```

---

## Transactional Outbox Pattern

Guarantees at-least-once domain event delivery even if the message broker crashes.

### How it works
```
DB Transaction:
  ├── Save business entity
  └── Save OutboxMessage record (same transaction)
           └── Background worker polls OutboxMessages
                      ├── Process message
                      └── Mark as processed
```

### Tables
```csharp
public class OutboxMessage
{
    public Guid Id { get; set; }
    public DateTime OccurredOn { get; set; }
    public string Type { get; set; } = string.Empty;  // domain event type name
    public string Data { get; set; } = string.Empty;  // JSON payload
    public DateTime? ProcessedOn { get; set; }
}

public class OutboxMessageConsumer
{
    public Guid Id { get; set; }
    public string ConsumerName { get; set; } = string.Empty;
}
```

### Saving an outbox message (in DbContext SaveChanges override)
```csharp
var domainEvents = ChangeTracker.Entries<AggregateRoot>()
    .Select(e => e.Entity)
    .SelectMany(a => {
        var events = a.DomainEvents;
        a.ClearDomainEvents();
        return events;
    });

foreach (var domainEvent in domainEvents)
{
    OutboxMessages.Add(new OutboxMessage {
        Id = Guid.NewGuid(),
        OccurredOn = DateTime.UtcNow,
        Type = domainEvent.GetType().Name,
        Data = JsonSerializer.Serialize(domainEvent)
    });
}
```

---

## Docker Compose — Full Setup

```yaml
# deploy/docker-compose.yml
version: '3.8'

networks:
  moriicoffee:
    driver: bridge

services:
  moriicoffee.db:
    image: mcr.microsoft.com/mssql/server:2019-latest
    environment:
      SA_PASSWORD: "YourStrong@Passw0rd"
      ACCEPT_EULA: "Y"
    ports:
      - "1433:1433"
    networks:
      - moriicoffee

  moriicoffee.cache:
    image: redis:7.0.15-alpine
    ports:
      - "6379:6379"
    networks:
      - moriicoffee

  moriicoffee.seq:
    image: datalust/seq:latest
    environment:
      ACCEPT_EULA: Y
    ports:
      - "5341:5341"
      - "3038:80"
    networks:
      - moriicoffee

  moriicoffee.hangfire:
    image: mongo:latest
    ports:
      - "27017:27017"
    networks:
      - moriicoffee

  moriicoffee.minio:
    image: minio/minio
    ports:
      - "9000:9000"
      - "9001:9001"
    environment:
      MINIO_ROOT_USER: minioadmin
      MINIO_ROOT_PASSWORD: minioadmin
    command: server /data --console-address ":9001"
    networks:
      - moriicoffee
```

```yaml
# deploy/docker-compose.development.yml
version: '3.8'

services:
  moriicoffee.api:
    build:
      context: ../source/MoriiCoffee.Presentation
      dockerfile: Dockerfile
    ports:
      - "8002:80"
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ASPNETCORE_URLS=http://+:80
    depends_on:
      - moriicoffee.db
      - moriicoffee.cache
    networks:
      - moriicoffee
```

## Multi-stage Dockerfile

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0-jammy AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY . .
RUN dotnet restore "MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj"
RUN dotnet publish "MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj" \
    -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MoriiCoffee.Presentation.dll"]
```

## Local Dev Setup Commands
```bash
# Start infrastructure
docker-compose -f deploy/docker-compose.yml -p moriicoffee up -d

# Apply migrations
dotnet ef database update \
  --startup-project source/MoriiCoffee.Presentation \
  --project source/MoriiCoffee.Infrastructure.Persistence

# Run API
dotnet run --project source/MoriiCoffee.Presentation
```

Swagger: `http://localhost:<port>/swagger/index.html`

## Serilog + Seq Logging

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration)
          .WriteTo.Console()
          .WriteTo.Seq(context.Configuration["SeqUrl"]!));
```

```json
// appsettings.Development.json
{
  "SeqUrl": "http://localhost:5341"
}
```
