---
name: morii-coffee-dotnet
description: >
  Architecture and coding patterns for the MoriiCoffee .NET 8 project. Use this skill whenever
  working on MoriiCoffee — adding features, creating aggregates, writing commands/queries,
  setting up infrastructure services, designing APIs, or making any architectural decision.
  Trigger on: new domain entity, new endpoint, new use case, CQRS handler, repository,
  EF Core config, SignalR hub, Redis caching, MinIO file storage, Hangfire jobs,
  authentication, Docker setup, or any "how should I structure X?" question for MoriiCoffee.
---

# MoriiCoffee — Architecture & Coding Reference

MoriiCoffee is a **backend-only REST API** for coffee shop management (products, orders, users, inventory). It follows **Clean Architecture + DDD + CQRS**, mirroring the EventHub reference project.

---

## Project Structure

```
MoriiCoffee/
├── deploy/
│   ├── docker-compose.yml                  # Infrastructure services
│   ├── docker-compose.development.yml      # Adds API container
│   └── docker-compose.production.yml
│
└── source/
    ├── MoriiCoffee.Domain/                 # Aggregates, repo interfaces, domain events
    ├── MoriiCoffee.Domain.Shared/          # Enums, constants, response wrappers, settings
    ├── MoriiCoffee.Application/            # CQRS handlers, DTOs, AutoMapper, validators
    ├── MoriiCoffee.Infrastructure/         # External service implementations, configs
    ├── MoriiCoffee.Infrastructure.Persistence/ # EF Core, repositories, migrations
    └── MoriiCoffee.Presentation/           # Controllers, middleware, Program.cs
```

**Dependency rule — one direction only:**
```
Presentation → Application → Domain ← Domain.Shared
                    ↑
              Infrastructure → Infrastructure.Persistence
```
Domain never imports EF Core, HTTP, Redis, or any external library. Infrastructure implements Domain interfaces.

---

## Technology Stack

| Concern | Technology | Version |
|---------|-----------|---------|
| Framework | ASP.NET Core | 8.0 |
| ORM | Entity Framework Core | 8.0+ |
| Micro-ORM | Dapper | 2.1+ |
| Database | SQL Server | 2019 |
| Cache | Redis | 7.x |
| Background Jobs | Hangfire + MongoDB | latest |
| File Storage | MinIO (S3-compatible) | latest |
| Logging | Serilog + Seq | 4.x |
| Real-time | SignalR | 8.0 |
| CQRS | MediatR | 12.4 |
| Validation | FluentValidation | 11.9 |
| Mapping | AutoMapper | 13.x |
| API Docs | Swashbuckle (Swagger) | 6.7 |
| Auth | ASP.NET Identity + JWT | 8.0 |

---

## Layer Responsibilities

### MoriiCoffee.Domain
Zero external dependencies. Contains:
- **Aggregates**: `Product`, `Order`, `User`, `Category` etc. — each with its own folder
- **Entities**: child objects owned by an aggregate (e.g., `OrderItem`, `ProductVariant`)
- **Value Objects**: identity-by-value join objects (e.g., `ProductCategory`)
- **Repository interfaces**: `IProductsRepository`, `IOrdersRepository` etc.
- **Base classes**: `AggregateRoot`, `Entity`, `IDomainEvent` in `SeedWork/`
- **Domain events**: fired from aggregate methods, processed via Outbox

### MoriiCoffee.Domain.Shared
Shared across all layers:
- **Enums**: `EProductStatus`, `EOrderStatus`, `EUserStatus`, etc.
- **Constants**: `CacheKeys`, `FileContainers`, `TokenTypes`
- **Response wrappers**: `ApiResponse<T>`, `ApiOkResponse`, `ApiBadRequestResponse`
- **Settings POCOs**: `JwtOptions`, `EmailSettings`, `MinioSettings`
- **Pagination**: `Pagination<T>`, `PaginationFilter`

### MoriiCoffee.Application
Depends on Domain only. Contains:
- **Commands** (write ops): one folder per command with Command + Handler + Validator
- **Queries** (read ops): one folder per query with Query + Handler
- **DTOs**: data shapes returned from handlers
- **AutoMapper profiles**: entity ↔ DTO mappings
- **Abstractions**: `IFileService`, `IEmailService`, `ITokenService`, `ICacheService`

### MoriiCoffee.Infrastructure
External adapters:
- Implements Application abstractions: `FileService` (MinIO), `EmailService`, `TokenService`, `CacheService`
- Configuration classes: `AuthenticationConfiguration`, `CachingConfiguration`, `HangfireConfiguration`, `SignalRConfiguration`, `SwaggerConfiguration`, etc.
- Outbox message processing

### MoriiCoffee.Infrastructure.Persistence
Data access:
- `ApplicationDbContext` (EF Core)
- Concrete repositories implementing Domain interfaces
- EF Core Fluent API configurations in `Configurations/`
- Database migrations
- Seed data
- Cached repository decorators (Redis-backed)

### MoriiCoffee.Presentation
HTTP boundary:
- API controllers (thin — just dispatch Commands/Queries via MediatR)
- `ErrorWrappingMiddleware` (global exception → `ApiResponse`)
- Email templates in `Templates/`
- `Program.cs` wires everything up

---

## Common Patterns

### Audit + Soft Delete (every aggregate root)
```csharp
DateTime CreatedAt { get; set; }
DateTime UpdatedAt { get; set; }
bool IsDeleted { get; set; }
DateTime? DeletedAt { get; set; }
```
Delete = flip `IsDeleted = true`. All queries filter `WHERE IsDeleted = false`.

### API Versioning
Base URL: `/api/v1/`

### HTTP Response Wrapper
All endpoints return `ApiResponse<T>`. Never return raw objects from controllers.

### Controller Pattern (thin)
```csharp
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductDto dto) {
    var command = _mapper.Map<CreateProductCommand>(dto);
    var result = await _mediator.Send(command);
    return Ok(new ApiOkResponse(result));
}
```

---

## Reference Files

Read these when working on specific areas:

- **`references/cqrs-patterns.md`** — Command/Query/Handler templates, validation, MediatR pipeline
- **`references/ddd-aggregates.md`** — Aggregate root structure, entities, value objects, repository interfaces, EF Core config
- **`references/infrastructure-services.md`** — Redis caching, MinIO file storage, SignalR hubs, Hangfire jobs, Docker setup, JWT auth, Outbox pattern

---

## Key Rules to Enforce

1. **Domain stays pure** — no EF Core, no HTTP, no external NuGet in `MoriiCoffee.Domain`
2. **One command/query = one folder** — `Commands/Product/CreateProduct/` contains `CreateProductCommand.cs`, `CreateProductCommandHandler.cs`, `CreateProductCommandValidator.cs`
3. **Commands return DTOs** (pragmatic relaxation of strict CQRS — matches EventHub convention)
4. **All writes go through Unit of Work** — `await _unitOfWork.CommitAsync()` at the end of handlers
5. **Cache invalidation on writes** — any Create/Update/Delete must invalidate relevant `CacheKeys`
6. **Soft delete everywhere** — never hard-delete aggregate roots unless explicitly requested
7. **FluentValidation for every command** — registered via `services.AddValidatorsFromAssembly()`
8. **AutoMapper for every DTO** — no manual mapping in handlers
