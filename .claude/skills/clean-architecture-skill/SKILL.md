---
name: morii-coffee-dotnet
description: >
  Architecture and coding patterns for the MoriiCoffee .NET 8 Clean Architecture project.
  Use this skill whenever working on MoriiCoffee — adding features, creating aggregates,
  writing commands/queries, repositories, EF Core configurations, infrastructure services,
  controllers, or making any architectural decision. Trigger on: new domain entity, new
  endpoint, new use case, CQRS handler, repository, EF Core config, SignalR hub, Redis
  caching, MinIO file storage, Hangfire jobs, authentication, migrations, seed data, Docker
  setup, or any "how should I structure X?" question for MoriiCoffee.
---

# MoriiCoffee — Clean Architecture Coding Standards

This skill captures the mandatory conventions for the MoriiCoffee .NET 8 project.
Follow every rule here whenever you create or modify any file in the solution.

---

## 1. XML Summary Documentation

Every file you create or modify must have `/// <summary>` comments on:
- The class / interface / enum / record itself
- Every non-trivial public property
- Every public method that is not self-evident

The summary explains the *what and why*, not just the name. One-liners are fine for simple
members; multi-line blocks are appropriate for complex types.

```csharp
using System.Security.Claims;
using MoriiCoffee.Domain.Aggregates.UserAggregate;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

/// <summary>Service for generating and validating JWT access tokens and opaque refresh tokens.</summary>
public interface ITokenService
{
    /// <summary>
    /// Asynchronously generates an access token for the specified user.
    /// </summary>
    /// <param name="user">
    /// The user for whom the access token is being generated. This user object typically contains user information 
    /// such as claims and roles.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a string representing the generated access token.
    /// </returns>

    Task<string> GenerateAccessTokenAsync(User user);

    /// <summary>
    /// Extracts the claims principal from the provided token.
    /// </summary>
    /// <param name="token">
    /// The JWT token from which to extract the claims principal. This token contains the claims associated with the user.
    /// </param>
    /// <returns>
    /// A <see cref="ClaimsIdentity"/> object representing the claims extracted from the token, or null if the token is invalid or cannot be parsed.
    /// </returns>
    Task<ClaimsIdentity?> GetPrincipalFromTokenAsync(string token);
}
```

Enums get a summary on the type; individual values only need comments when the name is ambiguous.

```csharp
/// <summary>Availability and visibility status of a product in the catalog.</summary>
public enum EProductStatus
{
    Active = 0,
    Inactive = 1,
    /// <summary>Temporarily hidden from customers but retained in the system.</summary>
    Archived = 2
}
```

---

## 2. Database Field Definitions with DataAnnotations

Define every database constraint **inside the Domain entity** using DataAnnotations.
Never rely on EF Core fluent API alone for basic column shape — keep it in the entity so the schema intent is visible without jumping to the persistence layer.

Configurations in Infrastructure.Persistence should ideally only define relationships and foreign keys; data type constraints should be implemented within the Aggregate.

| Scenario | Annotation |
|---|---|
| Primary key | `[Key]` |
| Required / NOT NULL | `[Required]` |
| Max string length | `[MaxLength(200)]` |
| Explicit column type | `[Column(TypeName = "nvarchar(200)")]` |
| Table name | `[Table("Products")]` |
| Decimal precision | `[Column(TypeName = "decimal(18,2)")]` |

```csharp
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Product;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace MoriiCoffee.Domain.Aggregates.ProductAggregate;

/// <summary>
/// Represents a product in the coffee shop catalog (e.g., "Caramel Latte").
/// Acts as the aggregate root for the Product bounded context.
/// Each product can have multiple <see cref="ProductVariant"/> (sizes/options)
/// and multiple <see cref="ProductImage"/> entries.
/// </summary>
[Table("Products")]
public class Product : AggregateRoot
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>Display name of the product (e.g., "Iced Caramel Macchiato").</summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// URL-friendly identifier for the product (e.g., "iced-caramel-macchiato").
    /// Used for SEO-friendly endpoints and frontend routing.
    /// </summary>
    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string Slug { get; set; } = null!;

    /// <summary>Full description of the product shown on the product detail page.</summary>
    [MaxLength(2000)]
    [Column(TypeName = "nvarchar(2000)")]
    public string? Description { get; set; }

    /// <summary>
    /// Base price of the product. Variants may add an additional price on top of this.
    /// </summary>
    [Required]
    [Column(TypeName = "decimal(18,2)")]
    public decimal BasePrice { get; set; }

    /// <summary>Collection of categories this product belongs to.</summary>
    public ICollection<ProductCategory> ProductCategories { get; set; } = new List<ProductCategory>();

    /// <summary>URL of the main thumbnail image for the product.</summary>
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? ThumbnailUrl { get; set; }

    /// <summary>Availability and visibility status of the product.</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EProductStatus Status { get; set; } = EProductStatus.Active;

    /// <summary>Whether this product should be highlighted on the home page or featured section.</summary>
    public bool IsFeatured { get; set; }

    /// <summary>Sort order for displaying products within a category.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Collection of size/option variants for this product.</summary>
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();

    /// <summary>Collection of additional gallery images for this product.</summary>
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
}
```


```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        // Relationships
        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId);
    }
}
```
---

## 3. SwaggerSchema should be added to each DTO in Application/DTOs. For example:
```csharp
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Application.SeedWork.DTOs.Auth;

/// <summary>Request body for the POST /auth/signin endpoint. Identity can be email or username.</summary>
public class SignInDto
{
    [SwaggerSchema("Phone number or email registered for the account")]
    public string Identity { get; set; } = null!;
    
    [SwaggerSchema("Password for the account")]
    public string Password { get; set; } = null!;
}

```

## 4. Relationship Configuration in Infrastructure.Persistence

Put **all** EF Core relationship configuration inside `IEntityTypeConfiguration<T>` classes
in the `MoriiCoffee.Infrastructure.Persistence/Configurations/` folder.
Never configure relationships inside Domain entities or ApplicationDbContext directly.

This keeps the Domain layer persistence-ignorant and makes EF mappings easy to find.

```csharp
/// <summary>EF Core configuration for the Product aggregate.</summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
```

ApplicationDbContext auto-discovers these via `ApplyConfigurationsFromAssembly`.
You only need to add a new configuration class — no changes to DbContext required.

---

## 5. Entity Base Requirements

Every class that lives inside an Aggregate must implement `IEntityBase`. Choose the
right base depending on the class's role:

### Option 1 — Aggregate root (owns a bounded context, dispatches domain events)

```csharp
[Table("Products")]
public class Product : AggregateRoot   // AggregateRoot extends EntityBase
{
    // ...
}
```

### Option 2 — Child entity (owned by an aggregate root, has its own identity)

```csharp
[Table("ProductVariants")]
public class ProductVariant : EntityBase
{
    // ...
}
```

### Option 3 — Identity-based aggregate root (avoids Id conflict with IdentityUser)

When a domain entity inherits from `IdentityUser<Guid>`, it cannot also inherit from
`AggregateRoot` because both define an `Id` property. Instead, implement both interfaces
directly and own the `_domainEvents` list yourself.

```csharp
/// <summary>
/// Represents a MoriiCoffee user. Extends IdentityUser to inherit Identity-managed
/// fields (password hash, security stamp, lockout, etc.) and adds domain-specific
/// fields. Implements IAggregateRoot directly to avoid Id conflict with EntityBase.
/// </summary>
public class User : IdentityUser<Guid>, IAggregateRoot, IEntityBase
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => _domainEvents.ToList();
    public void ClearDomainEvents() => _domainEvents.Clear();
    public void RaiseDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

    // Domain fields ...
}
```

Value objects that have no identity of their own (e.g., `Money`, `Address`) do not need
`IEntityBase` — they should not have a separate table.

---

## 6. Migration and Seed Rules

Any time you modify a class inside an Aggregate (add/remove/rename a property, change a
type, add a relationship), ask yourself two questions before finishing:

1. **Does this change require a new EF Core migration?**
   - Yes if a column, table, or foreign key changed.
   - No if you only changed business logic in a domain method.

2. **Does `ApplicationDbContextSeed` need updating?**
   - Yes if you added a required field without a default, changed enum values used in
     seed data, or added a new entity that should ship with initial data.

---

## 7. Creating a Migration

```powershell
dotnet ef migrations add <MigrationName> `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj `
  --output-dir Migrations
```

Name migrations descriptively: `AddOrderAggregate`, `AddUserAvatarColumn`,
`RenameProductSlugColumn` — not `Update1` or `Fix`.

> NOTE: Do NOT add `--connection` to `migrations add` — it is not a valid option and will cause an error.

---

## 8. Updating the Database (dev — uses appsettings connection string)

```powershell
dotnet ef database update `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
```

---

## 9. Applying a Migration with an Explicit Connection String

```powershell
dotnet ef database update `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj `
  --connection "Server=localhost\SQLEXPRESS;Database=MoriiCoffeeDb;Trusted_Connection=true;TrustServerCertificate=true;"
```

---

## 10. Build Verification — No Errors Before Shipping

After every implementation session — whether you added one file or a whole feature —
build the Presentation project (which pulls all layers) and confirm zero errors:

```powershell
dotnet build source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj --no-incremental
```

If the build fails, read the compiler output carefully, fix every error, and build again.
Repeat this fix → build loop until the output reads:

```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

Do not consider a task done until the solution compiles cleanly. A build with errors is
not shippable code, regardless of how correct the logic looks. Common things to check
when a build fails:

- Missing `using` directives or wrong namespaces
- Type alias needed (`using UserEntity = ...`) when a folder name collides with a type name
- New interface member added but not implemented in concrete classes
- New DI registration missing in `DependencyInjection.cs`
- AutoMapper `ForMember` ignore needed for properties that can't be auto-mapped

---

## 11. Quick Implementation Checklist

When generating or modifying any file, mentally run through this list:

- [ ] `/// <summary>` on the class/interface/enum/record
- [ ] `/// <summary>` on non-trivial public properties
- [ ] `[Table]`, `[Key]`, `[Required]`, `[MaxLength]`, `[Column(TypeName)]` on entity fields
- [ ] Relationships configured in `Configurations/<Entity>Configuration.cs`, not in the entity
- [ ] Correct base class: `AggregateRoot` / `EntityBase` / direct interface implementation
- [ ] New migration created if schema changed
- [ ] Seed data updated if required fields were added or enum values changed
- [ ] `dotnet build` passes with 0 errors and 0 warnings

---

## Project Layer Map (quick reference)

```
MoriiCoffee.Domain.Shared       → Enums, Constants, Settings, SeedWork types
MoriiCoffee.Domain              → Aggregates, Repository interfaces, Domain events
MoriiCoffee.Application         → Commands, Queries, Validators, DTOs, Abstractions
MoriiCoffee.Infrastructure      → TokenService, EmailService, FileService, DI wiring
MoriiCoffee.Infrastructure.Persistence → DbContext, EF Configurations, Repositories, UnitOfWork, Migrations
MoriiCoffee.Presentation        → Controllers, Middleware, ApplicationExtensions
```

Flow: **Presentation → Application → Domain ← Infrastructure.Persistence**
Dependencies always point inward. The Domain layer knows nothing about EF Core or ASP.NET.

---

## Running the Project

Two compose files — infrastructure is always separate from the API:
- `docker-compose.yml` — SQL Server + MinIO only
- `docker-compose.development.yml` — API service only

### Option 1 — Docker: infrastructure + API

```bash
cd deploy

# First time (build image)
docker compose -f docker-compose.yml -f docker-compose.development.yml up --build

# Subsequent runs
docker compose -f docker-compose.yml -f docker-compose.development.yml up -d

# Stop
docker compose -f docker-compose.yml -f docker-compose.development.yml down
```

### Option 2 — Local dotnet watch (recommended for active development)

```bash
cd deploy

# Start infrastructure only
docker compose up -d

# Run API locally with hot reload (from project root)
cd ..
dotnet watch --project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
```

> When running locally, `appsettings.Development.json` uses `moriicoffee.database` as the server name.
> Override via dotnet user-secrets if you need `localhost` instead.

---

## Local Port Reference

| Service | URL | Notes |
|---|---|---|
| API (Docker) | http://localhost:8002 | via docker-compose.development.yml |
| API (local) | http://localhost:5100 | via dotnet watch |
| Swagger | http://localhost:8002/swagger or http://localhost:5100/swagger | |
| MinIO API | http://localhost:9000 | S3-compatible endpoint |
| MinIO Console | http://localhost:9001 | login: `minioadmin` / `minioadmin` |
| SQL Server | `localhost,1433` | User: `sa` / `MoriiCoffee@123~` |

---

## Daily Development Workflow

### Start of day

```bash
open -a Docker   # open Docker Desktop if not auto-started

cd deploy
docker compose up -d   # start infrastructure (DB + MinIO)
```

### After adding new code — rebuild API container

```bash
cd deploy
docker compose -f docker-compose.yml -f docker-compose.development.yml up -d --build moriicoffee.api
```

### Create a new migration

```bash
# Run from project root
dotnet ef migrations add <MigrationName> \
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj \
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj \
  --output-dir Migrations
```

### Apply migrations to the database

```bash
# Run from project root — connects to SQL Server container via localhost
dotnet ef database update \
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj \
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj \
  --connection "Server=localhost,1433;Database=MoriiCoffeeDb;User Id=sa;Password=MoriiCoffee@123~;TrustServerCertificate=true;"
```

> The API applies pending migrations automatically on startup via `MigrateAsync()`.
> Run `database update` manually only when applying migrations outside of app startup.

### End of day

```bash
cd deploy
docker compose down   # stops containers, data is preserved in volumes
```

### Quick reference

| Situation | Command (from `deploy/`) |
|---|---|
| Start infrastructure | `docker compose up -d` |
| Start infrastructure + API | `docker compose -f docker-compose.yml -f docker-compose.development.yml up -d` |
| Rebuild API container | `docker compose -f docker-compose.yml -f docker-compose.development.yml up -d --build moriicoffee.api` |
| Create migration | `dotnet ef migrations add <Name> ...` (project root) |
| Apply migration | `dotnet ef database update ...` (project root) |
| Stop all | `docker compose down` |
