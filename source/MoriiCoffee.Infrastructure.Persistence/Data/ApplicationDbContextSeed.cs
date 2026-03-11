using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Domain.Shared.Enums.User;

namespace MoriiCoffee.Infrastructure.Persistence.Data;

/// <summary>
/// Seeds the database with roles, an admin user, and sample catalog data.
/// </summary>
public class ApplicationDbContextSeed
{
    private readonly ApplicationDbContext _context;
    private readonly RoleManager<Role> _roleManager;
    private readonly UserManager<User> _userManager;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ApplicationDbContextSeed> _logger;

    public ApplicationDbContextSeed(
        ApplicationDbContext context,
        RoleManager<Role> roleManager,
        UserManager<User> userManager,
        IConfiguration configuration,
        ILogger<ApplicationDbContextSeed> logger)
    {
        _context = context;
        _roleManager = roleManager;
        _userManager = userManager;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        await SeedRolesAsync();
        await SeedAdminUserAsync();
        await SeedSampleUsersAsync();
        await SeedCatalogAsync();
    }

    private async Task SeedRolesAsync()
    {
        string[] roles = Enum.GetNames<ERole>();
        foreach (var roleName in roles)
        {
            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                await _roleManager.CreateAsync(new Role(roleName));
                _logger.LogInformation("Created role: {Role}", roleName);
            }
        }
    }

    private async Task SeedAdminUserAsync()
    {
        var email = _configuration["AdminSeed:Email"] ?? "admin@moriicoffee.com";
        var password = _configuration["AdminSeed:Password"] ?? "Admin@123456";

        if (await _userManager.FindByEmailAsync(email) is not null)
            return;

        var now = DateTime.UtcNow;
        var admin = new User
        {
            Id = Guid.NewGuid(),
            UserName = "admin",
            Email = email,
            FullName = "MoriiCoffee Admin",
            Status = EUserStatus.Active,
            CreatedAt = now,
            UpdatedAt = now,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(admin, password);
        if (result.Succeeded)
        {
            await _userManager.AddToRoleAsync(admin, nameof(ERole.ADMIN));
            _logger.LogInformation("Seeded admin user: {Email}", email);
        }
        else
        {
            _logger.LogWarning("Failed to seed admin user: {Errors}",
                string.Join(", ", result.Errors.Select(e => e.Description)));
        }
    }

    private async Task SeedSampleUsersAsync()
    {
        var sampleUsers = new[]
        {
            new { Email = "staff@moriicoffee.com", UserName = "staff", FullName = "MoriiCoffee Staff", Role = nameof(ERole.STAFF) },
            new { Email = "customer1@gmail.com",   UserName = "customer1", FullName = "Nguyen Van A",    Role = nameof(ERole.CUSTOMER) },
            new { Email = "customer2@gmail.com",   UserName = "customer2", FullName = "Tran Thi B",      Role = nameof(ERole.CUSTOMER) },
        };

        var seedCredential = _configuration["AdminSeed:Password"] ?? "Sample@123456";

        foreach (var sample in sampleUsers)
        {
            if (await _userManager.FindByEmailAsync(sample.Email) is not null)
                continue;

            var user = new User
            {
                UserName = sample.UserName,
                Email = sample.Email,
                FullName = sample.FullName,
                Status = EUserStatus.Active,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, seedCredential);
            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, sample.Role);
                _logger.LogInformation("Seeded user: {Email} ({Role})", sample.Email, sample.Role);
            }
            else
            {
                _logger.LogWarning("Failed to seed user {Email}: {Errors}", sample.Email,
                    string.Join(", ", result.Errors.Select(e => e.Description)));
            }
        }
    }

    private async Task SeedCatalogAsync()
    {
        if (await _context.Categories.AnyAsync())
        {
            _logger.LogInformation("Catalog already seeded. Skipping.");
            return;
        }

        _logger.LogInformation("Seeding catalog data...");

        var categories = GetSeedCategories();
        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        var products = GetSeedProducts();
        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();

        var productCategories = GetSeedProductCategories(products, categories);
        await _context.ProductCategories.AddRangeAsync(productCategories);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Catalog seeded successfully.");
    }

    private static List<Category> GetSeedCategories()
    {
        DateTime now = DateTime.UtcNow;
        return new List<Category>
        {
            new() { Id = Guid.NewGuid(), Name = "Espresso", Description = "Classic espresso-based drinks", DisplayOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "Cold Brew", Description = "Smooth cold-steeped coffee", DisplayOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "Tea", Description = "Premium loose-leaf and specialty teas", DisplayOrder = 3, IsActive = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "Pastries", Description = "Freshly baked pastries and snacks", DisplayOrder = 4, IsActive = true, CreatedAt = now },
        };
    }

    private static List<Product> GetSeedProducts()
    {
        DateTime now = DateTime.UtcNow;
        var products = new List<Product>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Caramel Macchiato",
                Slug = "caramel-macchiato",
                Description = "Espresso with vanilla-flavored syrup, milk, and caramel drizzle.",
                BasePrice = 45_000m,
                Status = EProductStatus.Active,
                IsFeatured = true,
                DisplayOrder = 1,
                CreatedAt = now
            },
            new()
            {
                Id = Guid.NewGuid(),
                Name = "Classic Cold Brew",
                Slug = "classic-cold-brew",
                Description = "24-hour cold-steeped coffee for a smooth, low-acidity taste.",
                BasePrice = 55_000m,
                Status = EProductStatus.Active,
                IsFeatured = true,
                DisplayOrder = 1,
                CreatedAt = now
            }
        };

        foreach (var product in products)
        {
            product.Variants = new List<ProductVariant>
            {
                new() { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Small (8 oz)", Size = EProductSize.Small, AdditionalPrice = 0, IsDefault = true, IsAvailable = true, StockQuantity = -1, CreatedAt = now },
                new() { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Medium (12 oz)", Size = EProductSize.Medium, AdditionalPrice = 10_000m, IsDefault = false, IsAvailable = true, StockQuantity = -1, CreatedAt = now },
                new() { Id = Guid.NewGuid(), ProductId = product.Id, Name = "Large (16 oz)", Size = EProductSize.Large, AdditionalPrice = 20_000m, IsDefault = false, IsAvailable = true, StockQuantity = -1, CreatedAt = now },
            };
        }

        return products;
    }

    private static List<ProductCategory> GetSeedProductCategories(List<Product> products, List<Category> categories)
    {
        DateTime now = DateTime.UtcNow;
        var espresso = categories.First(c => c.Name == "Espresso");
        var coldBrew = categories.First(c => c.Name == "Cold Brew");

        var caramelMacchiato = products.First(p => p.Slug == "caramel-macchiato");
        var classicColdBrew = products.First(p => p.Slug == "classic-cold-brew");

        return new List<ProductCategory>
        {
            new() { CategoryId = espresso.Id, ProductId = caramelMacchiato.Id, CreatedAt = now },
            new() { CategoryId = coldBrew.Id, ProductId = classicColdBrew.Id, CreatedAt = now }
        };
    }
}
