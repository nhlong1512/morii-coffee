using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.Helpers;

// SQL Server DateTimes have no timezone info; tell Npgsql to accept Kind=Unspecified as UTC
AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

var options = MigrationOptions.Parse(args);

if (options.ShowHelp)
{
    Console.WriteLine(MigrationOptions.HelpText);
    return 0;
}

if (string.IsNullOrWhiteSpace(options.SourceConnectionString) ||
    string.IsNullOrWhiteSpace(options.TargetConnectionString))
{
    Console.Error.WriteLine("Both --source and --target connection strings are required.");
    Console.WriteLine(MigrationOptions.HelpText);
    return 1;
}

var sourceBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
sourceBuilder.UseSqlServer(options.SourceConnectionString);

var targetBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
targetBuilder
    .UseNpgsql(options.TargetConnectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
        // No EnableRetryOnFailure — NpgsqlRetryingExecutionStrategy conflicts with user-initiated transactions
    })
    .ConfigureWarnings(w => w.Ignore(RelationalEventId.PendingModelChangesWarning))
    .AddInterceptors(new NormalizeDateTimesInterceptor());

await using var source = new ApplicationDbContext(sourceBuilder.Options);
await using var target = new ApplicationDbContext(targetBuilder.Options);

if (options.ResetTarget)
{
    Console.WriteLine("Resetting PostgreSQL target database...");
    await target.Database.EnsureDeletedAsync();
}

Console.WriteLine("Applying PostgreSQL migrations...");
await target.Database.MigrateAsync();

if (!options.ResetTarget && await HasExistingDataAsync(target))
{
    Console.Error.WriteLine("Target database already contains data. Re-run with --reset-target to recreate it before migration.");
    return 1;
}

Console.WriteLine("Loading source data from SQL Server...");

var roleClaims = await source.Set<IdentityRoleClaim<Guid>>()
    .AsNoTracking()
    .ToListAsync();
var roles = await source.Roles
    .AsNoTracking()
    .ToListAsync();
var users = await source.Users
    .AsNoTracking()
    .ToListAsync();
var userClaims = await source.Set<IdentityUserClaim<Guid>>()
    .AsNoTracking()
    .ToListAsync();
var userLogins = await source.Set<IdentityUserLogin<Guid>>()
    .AsNoTracking()
    .ToListAsync();
var userRoles = await source.Set<IdentityUserRole<Guid>>()
    .AsNoTracking()
    .ToListAsync();
var userTokens = await source.Set<IdentityUserToken<Guid>>()
    .AsNoTracking()
    .ToListAsync();
var userDeliveryProfiles = await source.UserDeliveryProfiles
    .AsNoTracking()
    .ToListAsync();
var categories = await source.Categories
    .AsNoTracking()
    .ToListAsync();
var products = await source.Products
    .AsNoTracking()
    .ToListAsync();
var productCategories = await source.ProductCategories
    .AsNoTracking()
    .ToListAsync();
var productVariants = await source.ProductVariants
    .AsNoTracking()
    .ToListAsync();
var productImages = await source.ProductImages
    .AsNoTracking()
    .ToListAsync();
var banners = await source.Banners
    .AsNoTracking()
    .ToListAsync();
var orders = await source.Orders
    .Include(o => o.Items)
    .AsNoTracking()
    .ToListAsync();

NormalizeAll(
    roles,
    roleClaims,
    users,
    userClaims,
    userLogins,
    userRoles,
    userTokens,
    userDeliveryProfiles,
    categories,
    products,
    productCategories,
    productVariants,
    productImages,
    banners,
    orders);

target.ChangeTracker.AutoDetectChangesEnabled = false;

Console.WriteLine("Writing data to PostgreSQL...");
await using var transaction = await target.Database.BeginTransactionAsync();
try
{
    await target.Roles.AddRangeAsync(roles);
    await target.SaveChangesAsync();

    await target.AddRangeAsync(roleClaims);
    await target.SaveChangesAsync();

    await target.Users.AddRangeAsync(users);
    await target.SaveChangesAsync();

    await target.AddRangeAsync(userClaims);
    await target.SaveChangesAsync();

    await target.AddRangeAsync(userLogins);
    await target.SaveChangesAsync();

    await target.AddRangeAsync(userRoles);
    await target.SaveChangesAsync();

    await target.AddRangeAsync(userTokens);
    await target.SaveChangesAsync();

    await target.UserDeliveryProfiles.AddRangeAsync(userDeliveryProfiles);
    await target.SaveChangesAsync();

    await target.Categories.AddRangeAsync(categories);
    await target.SaveChangesAsync();

    await target.Products.AddRangeAsync(products);
    await target.SaveChangesAsync();

    await target.ProductCategories.AddRangeAsync(productCategories);
    await target.SaveChangesAsync();

    await target.ProductVariants.AddRangeAsync(productVariants);
    await target.SaveChangesAsync();

    await target.ProductImages.AddRangeAsync(productImages);
    await target.SaveChangesAsync();

    await target.Banners.AddRangeAsync(banners);
    await target.SaveChangesAsync();

    await target.Orders.AddRangeAsync(orders);
    await target.SaveChangesAsync();

    await ReseedIdentitySequencesAsync(target);

    await transaction.CommitAsync();
}
catch
{
    await transaction.RollbackAsync();
    throw;
}

Console.WriteLine("Migration completed successfully.");
Console.WriteLine($"Users: {users.Count}, Categories: {categories.Count}, Products: {products.Count}, Orders: {orders.Count}");
return 0;

static async Task<bool> HasExistingDataAsync(ApplicationDbContext target)
{
    return await target.Users.AnyAsync()
        || await target.Categories.AnyAsync()
        || await target.Products.AnyAsync()
        || await target.Orders.AnyAsync()
        || await target.Banners.AnyAsync();
}

static void NormalizeAll(params IEnumerable<object>[] entityGroups)
{
    foreach (var entityGroup in entityGroups)
    {
        foreach (var entity in entityGroup)
        {
            DateTimeNormalizationHelper.NormalizeObjectGraph(entity);
        }
    }
}

static async Task ReseedIdentitySequencesAsync(ApplicationDbContext target)
{
    string[] identityTables =
    [
        "AspNetRoleClaims",
        "AspNetUserClaims"
    ];

    foreach (var tableName in identityTables)
    {
        var sql = $"""
            SELECT setval(
                pg_get_serial_sequence('"{tableName}"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "{tableName}"), 0) + 1,
                false);
            """;

        await target.Database.ExecuteSqlRawAsync(sql);
    }
}

// Normalizes DateTime Kind=Unspecified → UTC when EF saves to PostgreSQL.
// Does NOT overwrite CreatedAt/UpdatedAt so original timestamps are preserved.
internal sealed class NormalizeDateTimesInterceptor : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null)
        {
            foreach (var entry in eventData.Context.ChangeTracker.Entries())
                DateTimeNormalizationHelper.NormalizeTrackedDateTimes(entry);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}

internal sealed record MigrationOptions(
    string? SourceConnectionString,
    string? TargetConnectionString,
    bool ResetTarget,
    bool ShowHelp)
{
    public const string HelpText = """
Usage:
  dotnet run --project source/MoriiCoffee.DbMigrator -- \
    --source "<sql-server-connection-string>" \
    --target "<postgres-connection-string>" \
    [--reset-target]

Options:
  --source        SQL Server connection string to migrate from.
  --target        PostgreSQL connection string to migrate into.
  --reset-target  Deletes and recreates the PostgreSQL database before importing data.
  --help          Show this message.
""";

    public static MigrationOptions Parse(string[] args)
    {
        string? source = null;
        string? target = null;
        var resetTarget = false;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--source" when i + 1 < args.Length:
                    source = args[++i];
                    break;
                case "--target" when i + 1 < args.Length:
                    target = args[++i];
                    break;
                case "--reset-target":
                    resetTarget = true;
                    break;
                case "--help":
                case "-h":
                    showHelp = true;
                    break;
            }
        }

        return new MigrationOptions(source, target, resetTarget, showHelp);
    }
}
