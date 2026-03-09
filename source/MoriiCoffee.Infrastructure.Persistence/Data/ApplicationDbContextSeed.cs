using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.Shared.Enums.Product;

namespace MoriiCoffee.Infrastructure.Persistence.Data;

/// <summary>
/// Seeds the database with realistic sample data for development and testing.
/// Only runs when the database is empty.
/// </summary>
public class ApplicationDbContextSeed
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ApplicationDbContextSeed> _logger;

    public ApplicationDbContextSeed(ApplicationDbContext context, ILogger<ApplicationDbContextSeed> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync()
    {
        if (await _context.Categories.AnyAsync())
        {
            _logger.LogInformation("Database already seeded. Skipping.");
            return;
        }

        _logger.LogInformation("Seeding database with sample data...");

        var categories = GetSeedCategories();
        await _context.Categories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        var products = GetSeedProducts();
        await _context.Products.AddRangeAsync(products);
        await _context.SaveChangesAsync();

        var productCategories = GetSeedProductCategories(products, categories);
        await _context.ProductCategories.AddRangeAsync(productCategories);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Database seeded successfully.");
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

        // Add variants to each product
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
