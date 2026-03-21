using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
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
        await SeedBannersAsync();
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
            new() { Id = Guid.NewGuid(), Name = "espresso",     Description = "Classic espresso-based drinks",          DisplayOrder = 1, IsActive = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "cold-brew",   Description = "Smooth cold-steeped coffee",             DisplayOrder = 2, IsActive = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "latte",       Description = "Creamy espresso-based lattes",           DisplayOrder = 3, IsActive = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "pastry",      Description = "Freshly baked pastries and snacks",      DisplayOrder = 4, IsActive = true, CreatedAt = now },
            new() { Id = Guid.NewGuid(), Name = "merchandise", Description = "Morii Coffee branded merchandise",       DisplayOrder = 5, IsActive = true, CreatedAt = now },
        };
    }

    private static List<Product> GetSeedProducts()
    {
        DateTime now = DateTime.UtcNow;

        // ── Espresso ──────────────────────────────────────────────────────────
        var classicEspresso = MakeProduct("Classic Espresso", "classic-espresso",
            "A rich, full-bodied espresso made from our signature blend of single-origin Arabica beans. Bold, smooth, and perfectly balanced with notes of dark chocolate and caramel.",
            85_000m, featured: true, order: 1, now);
        classicEspresso.Variants = BuildVariants(classicEspresso.Id, new[] { EProductSize.Small, EProductSize.Medium }, now);

        var doubleShotEspresso = MakeProduct("Double Shot Espresso", "double-shot-espresso",
            "Twice the intensity, twice the flavor. Our double shot espresso delivers a powerful caffeine kick with a velvety crema and deep roasted aroma.",
            115_000m, featured: false, order: 2, now);
        doubleShotEspresso.Variants = BuildVariants(doubleShotEspresso.Id, new[] { EProductSize.Small, EProductSize.Medium }, now);

        // ── Cold Brew ─────────────────────────────────────────────────────────
        var vanillaColdBrew = MakeProduct("Vanilla Cold Brew", "vanilla-cold-brew",
            "Slow-steeped for 20 hours, our cold brew is naturally sweet and incredibly smooth. Infused with real Madagascar vanilla for a creamy, refreshing finish.",
            125_000m, featured: true, order: 1, now);
        vanillaColdBrew.Variants = BuildVariants(vanillaColdBrew.Id, new[] { EProductSize.Medium, EProductSize.Large, EProductSize.ExtraLarge }, now);

        var nitroColdBrew = MakeProduct("Nitro Cold Brew", "nitro-cold-brew",
            "Our signature cold brew infused with nitrogen for a cascading, creamy texture. Silky smooth with a naturally sweet taste and no added sugar.",
            135_000m, featured: false, order: 2, now);
        nitroColdBrew.Variants = BuildVariants(nitroColdBrew.Id, new[] { EProductSize.Medium, EProductSize.Large }, now);

        var mochaColdBrew = MakeProduct("Mocha Cold Brew", "mocha-cold-brew",
            "Rich cold brew coffee blended with dark chocolate and a touch of vanilla. Served over ice for the ultimate chocolate-coffee indulgence.",
            135_000m, featured: true, order: 3, now);
        mochaColdBrew.Variants = BuildVariants(mochaColdBrew.Id, new[] { EProductSize.Medium, EProductSize.Large, EProductSize.ExtraLarge }, now);

        // ── Latte ─────────────────────────────────────────────────────────────
        var caramelLatte = MakeProduct("Caramel Latte", "caramel-latte",
            "Espresso meets steamed milk and our house-made caramel sauce. A sweet, indulgent treat topped with a drizzle of caramel and a sprinkle of sea salt.",
            135_000m, featured: true, order: 1, now);
        caramelLatte.Variants = BuildVariants(caramelLatte.Id, new[] { EProductSize.Small, EProductSize.Medium, EProductSize.Large }, now);

        var matchaLatte = MakeProduct("Matcha Latte", "matcha-latte",
            "Ceremonial-grade Japanese matcha whisked with steamed oat milk. Earthy, creamy, and energizing with a vibrant green hue.",
            150_000m, featured: true, order: 2, now);
        matchaLatte.Variants = BuildVariants(matchaLatte.Id, new[] { EProductSize.Small, EProductSize.Medium, EProductSize.Large }, now);

        var oatMilkLatte = MakeProduct("Oat Milk Latte", "oat-milk-latte",
            "Our smooth espresso paired with creamy oat milk. A plant-based delight that is rich, satisfying, and naturally sweet.",
            125_000m, featured: true, order: 3, now);
        oatMilkLatte.Variants = BuildVariants(oatMilkLatte.Id, new[] { EProductSize.Small, EProductSize.Medium, EProductSize.Large }, now);

        var honeyLavenderLatte = MakeProduct("Honey Lavender Latte", "honey-lavender-latte",
            "A floral twist on the classic latte. Local honey and French lavender blended with espresso and steamed milk for a calming, aromatic experience.",
            165_000m, featured: true, order: 4, now);
        honeyLavenderLatte.Variants = BuildVariants(honeyLavenderLatte.Id, new[] { EProductSize.Small, EProductSize.Medium, EProductSize.Large }, now);

        // ── Pastry ────────────────────────────────────────────────────────────
        var butterCroissant = MakeProduct("Butter Croissant", "butter-croissant",
            "Flaky, golden, and made with French butter. Our croissants are baked fresh every morning for the perfect coffee companion.",
            85_000m, featured: true, order: 1, now);
        butterCroissant.Variants = BuildStandardVariant(butterCroissant.Id, now);

        var cinnamonRoll = MakeProduct("Cinnamon Roll", "cinnamon-roll",
            "A warm, soft cinnamon roll drizzled with cream cheese frosting. Baked with layers of cinnamon sugar and a hint of cardamom.",
            100_000m, featured: false, order: 2, now);
        cinnamonRoll.Variants = BuildStandardVariant(cinnamonRoll.Id, now);

        var blueberryMuffin = MakeProduct("Blueberry Muffin", "blueberry-muffin",
            "Loaded with fresh blueberries and topped with a golden streusel crumble. Moist, tender, and bursting with fruity flavor.",
            75_000m, featured: false, order: 3, now, status: EProductStatus.OutOfStock);
        blueberryMuffin.Variants = BuildStandardVariant(blueberryMuffin.Id, now, isAvailable: false);

        // ── Merchandise ───────────────────────────────────────────────────────
        var moriiMug = MakeProduct("Morii Coffee Mug", "morii-coffee-mug",
            "A handcrafted ceramic mug featuring the Morii Coffee logo. 12oz capacity, microwave and dishwasher safe. Available in matte black.",
            450_000m, featured: false, order: 1, now);
        moriiMug.Variants = BuildStandardVariant(moriiMug.Id, now);

        var moriiToteBag = MakeProduct("Morii Tote Bag", "morii-tote-bag",
            "An organic cotton tote bag with our signature Morii Coffee print. Sturdy, eco-friendly, and perfect for carrying your daily essentials.",
            600_000m, featured: false, order: 2, now);
        moriiToteBag.Variants = BuildStandardVariant(moriiToteBag.Id, now);

        return new List<Product>
        {
            classicEspresso, doubleShotEspresso,
            vanillaColdBrew, nitroColdBrew, mochaColdBrew,
            caramelLatte, matchaLatte, oatMilkLatte, honeyLavenderLatte,
            butterCroissant, cinnamonRoll, blueberryMuffin,
            moriiMug, moriiToteBag,
        };
    }

    /// <summary>Creates a Product entity with common defaults.</summary>
    private static Product MakeProduct(
        string name, string slug, string description, decimal basePrice,
        bool featured, int order, DateTime now,
        EProductStatus status = EProductStatus.Active)
    {
        return new Product
        {
            Id = Guid.NewGuid(),
            Name = name,
            Slug = slug,
            Description = description,
            BasePrice = basePrice,
            Status = status,
            IsFeatured = featured,
            DisplayOrder = order,
            CreatedAt = now,
        };
    }

    /// <summary>
    /// Builds size variants for a product. The first size in the array is marked as default.
    /// Additional price follows standard VND tier: Small=0, Medium=+10k, Large=+20k, ExtraLarge=+30k.
    /// </summary>
    private static List<ProductVariant> BuildVariants(
        Guid productId, EProductSize[] sizes, DateTime now)
    {
        var sizeNames = new Dictionary<EProductSize, string>
        {
            { EProductSize.Small,      "Small (8 oz)"        },
            { EProductSize.Medium,     "Medium (12 oz)"      },
            { EProductSize.Large,      "Large (16 oz)"       },
            { EProductSize.ExtraLarge, "Extra Large (20 oz)" },
        };

        var sizePrices = new Dictionary<EProductSize, decimal>
        {
            { EProductSize.Small,      0m        },
            { EProductSize.Medium,     10_000m   },
            { EProductSize.Large,      20_000m   },
            { EProductSize.ExtraLarge, 30_000m   },
        };

        return sizes.Select((size, idx) => new ProductVariant
        {
            Id              = Guid.NewGuid(),
            ProductId       = productId,
            Name            = sizeNames[size],
            Size            = size,
            AdditionalPrice = sizePrices[size],
            IsDefault       = idx == 0,
            IsAvailable     = true,
            StockQuantity   = -1,
            CreatedAt       = now,
        }).ToList();
    }

    /// <summary>
    /// Builds a single "Standard" variant for products that have no size options
    /// (e.g., pastries and merchandise).
    /// </summary>
    private static List<ProductVariant> BuildStandardVariant(
        Guid productId, DateTime now, bool isAvailable = true)
    {
        return new List<ProductVariant>
        {
            new()
            {
                Id              = Guid.NewGuid(),
                ProductId       = productId,
                Name            = "Standard",
                Size            = EProductSize.Small,
                AdditionalPrice = 0m,
                IsDefault       = true,
                IsAvailable     = isAvailable,
                StockQuantity   = -1,
                CreatedAt       = now,
            }
        };
    }

    private static List<ProductCategory> GetSeedProductCategories(
        List<Product> products, List<Category> categories)
    {
        DateTime now = DateTime.UtcNow;

        // Build a lookup by category name for readability.
        var catByName = categories.ToDictionary(c => c.Name);

        // Map each product slug to the category it belongs to.
        var slugToCategory = new Dictionary<string, string>
        {
            { "classic-espresso",      "espresso"     },
            { "double-shot-espresso",  "espresso"     },
            { "vanilla-cold-brew",     "cold-brew"    },
            { "nitro-cold-brew",       "cold-brew"    },
            { "mocha-cold-brew",       "cold-brew"    },
            { "caramel-latte",         "latte"        },
            { "matcha-latte",          "latte"        },
            { "oat-milk-latte",        "latte"        },
            { "honey-lavender-latte",  "latte"        },
            { "butter-croissant",      "pastry"       },
            { "cinnamon-roll",         "pastry"       },
            { "blueberry-muffin",      "pastry"       },
            { "morii-coffee-mug",      "merchandise"  },
            { "morii-tote-bag",        "merchandise"  },
        };

        var result = new List<ProductCategory>();
        foreach (var product in products)
        {
            if (!slugToCategory.TryGetValue(product.Slug, out var catName))
                continue;

            if (!catByName.TryGetValue(catName, out var category))
                continue;

            result.Add(new ProductCategory
            {
                CategoryId = category.Id,
                ProductId  = product.Id,
                CreatedAt  = now,
            });
        }

        return result;
    }

    private async Task SeedBannersAsync()
    {
        if (await _context.Banners.AnyAsync())
        {
            _logger.LogInformation("Banners already seeded. Skipping.");
            return;
        }

        _logger.LogInformation("Seeding banner data...");

        var banners = GetSeedBanners();
        await _context.Banners.AddRangeAsync(banners);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Banners seeded successfully.");
    }

    private static List<Banner> GetSeedBanners()
    {
        DateTime now = DateTime.UtcNow;
        return new List<Banner>
        {
            new()
            {
                Id           = Guid.NewGuid(),
                Title        = "Savor the Moment",
                Subtitle     = "Artisan coffee crafted with passion. Experience the perfect blend of tradition and innovation.",
                Cta          = "Shop Now",
                CtaLink      = "/products",
                DisplayOrder = 1,
                StartDate    = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate      = new DateTime(2026, 12, 31, 23, 59, 59, DateTimeKind.Utc),
                IsActive     = true,
                CreatedAt    = now,
            },
            new()
            {
                Id           = Guid.NewGuid(),
                Title        = "New Seasonal Blend",
                Subtitle     = "Introducing our Spring Blossom blend — floral, bright, and delicately sweet. Available for a limited time.",
                Cta          = "Discover",
                CtaLink      = "/products/spring-blossom-blend",
                DisplayOrder = 2,
                StartDate    = new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate      = new DateTime(2026, 5, 31, 23, 59, 59, DateTimeKind.Utc),
                IsActive     = true,
                CreatedAt    = now,
            },
            new()
            {
                Id           = Guid.NewGuid(),
                Title        = "Earn Rewards with Every Sip",
                Subtitle     = "Join the Morii Loyalty program and unlock exclusive perks, free drinks, and early access to new releases.",
                Cta          = "Join Now",
                CtaLink      = "/loyalty",
                DisplayOrder = 3,
                StartDate    = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc),
                EndDate      = new DateTime(2026, 6, 30, 23, 59, 59, DateTimeKind.Utc),
                IsActive     = true,
                CreatedAt    = now,
            },
            new()
            {
                Id           = Guid.NewGuid(),
                Title        = "Visit Our New Location",
                Subtitle     = "Our newest cafe in District 1, Ho Chi Minh City is now open. Come in for a complimentary tasting.",
                Cta          = "Find Us",
                CtaLink      = "/stores",
                DisplayOrder = 4,
                StartDate    = new DateTime(2026, 3, 15, 0, 0, 0, DateTimeKind.Utc),
                EndDate      = new DateTime(2026, 4, 30, 23, 59, 59, DateTimeKind.Utc),
                IsActive     = true,
                CreatedAt    = now,
            },
        };
    }
}
