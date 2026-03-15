# Database Migration Guide - MoriiCoffee

This guide describes how to perform database migrations in the MoriiCoffee project. Migrations allow us to manage database schema changes safely and version-controlled through code.

## Database Architecture

- **Database Provider**: SQL Server
- **ORM**: Entity Framework Core 8.0
- **DbContext**: `ApplicationDbContext` (in `Infrastructure.Persistence.Data`)
- **Migrations Folder**: `source/MoriiCoffee.Infrastructure.Persistence/Migrations`
- **Configuration**: Connection string in `appsettings.json` or `appsettings.Development.json`

## Initial Setup Steps

### 1. Install EF Core CLI Tool (one-time)

Run the following command to install the EF Core Command Line Interface:

```powershell
dotnet tool install --global dotnet-ef
```

If already installed, update to the latest version:

```powershell
dotnet tool update --global dotnet-ef
```

**Note**: This only needs to be done once per machine.

### 2. Create the Initial Migration

From the **project root directory** (where `MoriiCoffee.slnx` is located), run:

```powershell
dotnet ef migrations add InitialCreate `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj `
  --output-dir Migrations `
  --connection "Server=localhost\SQLEXPRESS;Database=MoriiCoffeeDb;Trusted_Connection=true;TrustServerCertificate=true;"
```

**Explanation**:
- `InitialCreate`: Name of the migration (can be changed)
- `--project`: Project containing the `DbContext` (Infrastructure.Persistence)
- `--startup-project`: Startup project (Presentation)
- `--output-dir`: Folder for migration files (will be created at `source/MoriiCoffee.Infrastructure.Persistence/Migrations`)

**Result**: Two migration files will be created:
- `<timestamp>_InitialCreate.cs` - Contains Up/Down migration code
- `ApplicationDbContextModelSnapshot.cs` - Current schema snapshot

### 3. Apply Migration to the Database

Run the following command to create/update the database:

```powershell
dotnet ef database update `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
```

**What happens**:
1. The database will be created (if it doesn't exist)
2. Schema will be created according to `DbContext` and all `IEntityTypeConfiguration<T>`
3. A table `__EFMigrationsHistory` will be created to track applied migrations
4. Seeding data will run automatically (see "Auto-Migration & Seeding" section)

### 4. Run the API

```powershell
dotnet run --project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
```

The API will:
1. Start on `http://localhost:<port>`
2. Automatically check and apply any pending migrations
3. Automatically seed initial data
4. Redirect to Swagger UI at `/swagger`

## Common Scenarios

### Adding a New Entity or Changing Schema

**Step 1**: Update `DbContext` or create a new `IEntityTypeConfiguration<T>`

Example:
```csharp
// ApplicationDbContext.cs
public DbSet<NewEntity> NewEntities { get; set; }

// NewEntityConfiguration.cs (in Configurations folder)
public class NewEntityConfiguration : IEntityTypeConfiguration<NewEntity>
{
    public void Configure(EntityTypeBuilder<NewEntity> builder)
    {
        builder.ToTable("NewEntities");
        // ... other configurations
    }
}
```

**Step 2**: Create a new migration

```powershell
dotnet ef migrations add AddNewEntity `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj `
  --output-dir Migrations `
  --connection "Server=localhost\SQLEXPRESS;Database=MoriiCoffeeDb;Trusted_Connection=true;TrustServerCertificate=true;"
```

**Step 2**: Update a new migration
```powershell
dotnet ef database update `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj

```
**Step 3**: Apply the migration

```powershell
dotnet ef database update `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj `
  --connection "Server=localhost\SQLEXPRESS;Database=MoriiCoffeeDb;Trusted_Connection=true;TrustServerCertificate=true;"
```


### Drop & Recreate Database 
dotnet ef database drop `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj `
  --force


dotnet ef database update `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj

### Check Pending Migrations

To see if there are any migrations not yet applied:

```powershell
dotnet ef migrations list `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
```

### Rollback Migration

To revert one or more migrations:

```powershell
dotnet ef database update <migration_name> `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
```

Example: Roll back to the `InitialCreate` migration

```powershell
dotnet ef database update InitialCreate `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
```

### Remove Unapplied Migration

If a migration has not been applied to the database, you can remove it:

```powershell
dotnet ef migrations remove `
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj `
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj
```

## Auto-Migration & Seeding

When the API starts, code in `ApplicationExtensions.cs` will:

```csharp
// 1. Check for pending migrations
if (dbContext.Database.GetPendingMigrations().Any())
{
    dbContext.Database.Migrate();
    logger.LogInformation("Database migrations applied.");
}

// 2. Seed initial data
var seeder = services.GetRequiredService<ApplicationDbContextSeed>();
seeder.SeedAsync().GetAwaiter().GetResult();
```

**Benefits**:
- Every time you deploy or start the API, migrations are applied automatically
- Seed data is added to the database
- No need to run manual commands after deployment

## Connection String Configuration

The connection string is taken from `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnectionString": "Server=localhost\SQLEXPRESS;Database=MoriiCoffeeDb;Trusted_Connection=true;TrustServerCertificate=true;"
  }
}
```

**Notes**:
- Use `Trusted_Connection=true` for local development (Windows Authentication)
- Change Server/Database name as needed
- For production, use environment-specific `appsettings.Production.json`

## Troubleshooting

### Error: "No database provider has been configured"

**Cause**: Connection string not configured correctly or DbContext not registered

**Solution**:
1. Check that `appsettings.json` contains the connection string
2. Check that `ConfigureApplicationDbContext` in `Infrastructure.Configurations` is called in DI setup

### Error: "The migration 'X' has already been applied to the database"

**Cause**: Migration has already been applied, cannot apply again

**Solution**: Check the `__EFMigrationsHistory` table or use `dotnet ef migrations list` to see applied migrations

### Error: "Unable to create migrations because the current directory is not a valid .NET project"

**Cause**: Not running the command from the root folder

**Solution**: Run all commands from the root folder (where `MoriiCoffee.slnx` is located)

## Best Practices

1. **Commit migrations to source control**: Migrations are part of the codebase and should be committed to Git
2. **One migration per feature**: Create separate migrations for different features
3. **Review migrations before merging**: Check migration files to ensure schema changes are as intended
4. **Test migrations**: Run migrations on your local environment before pushing
5. **Use meaningful migration names**: Use descriptive names (e.g., `AddProductImageTable`, `AddCategoryDescriptionColumn`)
6. **Avoid seeding data in migrations**: Use `ApplicationDbContextSeed` for seeding, not migration files

## Further References

- [Entity Framework Core Migrations Documentation](https://learn.microsoft.com/en-us/ef/core/managing-schemas/migrations/)
- [Entity Framework Core Tools Reference](https://learn.microsoft.com/en-us/ef/core/cli/dotnet)
- [MoriiCoffee DbContext](../source/MoriiCoffee.Infrastructure.Persistence/Data/ApplicationDbContext.cs)
- [MoriiCoffee Configuration](../source/MoriiCoffee.Infrastructure/Configurations/)
