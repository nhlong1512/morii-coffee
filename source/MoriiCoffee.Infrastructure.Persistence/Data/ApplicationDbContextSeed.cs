using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Shared.Enums.Blog;
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
        await SeedBlogsAsync();
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

    private async Task SeedBlogsAsync()
    {
        if (await _context.BlogCategories.AnyAsync())
        {
            _logger.LogInformation("Blog content already seeded. Skipping.");
            return;
        }

        _logger.LogInformation("Seeding blog categories and posts...");

        var categories = GetSeedBlogCategories();
        await _context.BlogCategories.AddRangeAsync(categories);
        await _context.SaveChangesAsync();

        var posts = GetSeedBlogPosts();
        await _context.BlogPosts.AddRangeAsync(posts);
        await _context.SaveChangesAsync();

        var postCategories = GetSeedBlogPostCategories(posts, categories);
        await _context.Set<BlogPostCategory>().AddRangeAsync(postCategories);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Blog content seeded successfully.");
    }

    private static List<BlogCategory> GetSeedBlogCategories()
    {
        return new List<BlogCategory>
        {
            BlogCategory.Create(
                name: "Coffee Education",
                slug: "coffee-education",
                description: "Learn about coffee origins, brewing techniques, and flavor profiles.",
                displayOrder: 1,
                isActive: true),
            BlogCategory.Create(
                name: "Recipes & Ideas",
                slug: "recipes-ideas",
                description: "Discover creative ways to enjoy coffee at home.",
                displayOrder: 2,
                isActive: true),
            BlogCategory.Create(
                name: "Sustainability",
                slug: "sustainability",
                description: "Our commitment to ethical sourcing and environmental responsibility.",
                displayOrder: 3,
                isActive: true),
            BlogCategory.Create(
                name: "Cafe News",
                slug: "cafe-news",
                description: "Latest updates from Morii Coffee locations and events.",
                displayOrder: 4,
                isActive: true),
        };
    }

    private static List<BlogPost> GetSeedBlogPosts()
    {
        var now = DateTime.UtcNow;
        return new List<BlogPost>
        {
            // Coffee Education posts
            BlogPost.Create(
                title: "The Art of Espresso Extraction: A Complete Guide",
                slug: "espresso-extraction-guide",
                excerpt: "Master the fundamentals of espresso making and discover why timing, pressure, and grind size matter more than you think.",
                contentJson: null,
                contentHtml: @"<h2>Understanding Espresso Extraction</h2>
<p>Espresso is one of the most precise brewing methods in the coffee world. Every variable—from water temperature to grind fineness—plays a crucial role in creating that perfect shot.</p>
<h3>The Golden Rule: 25-30 Seconds</h3>
<p>The ideal espresso extraction time is between 25 and 30 seconds. Shots pulled faster than this are considered 'under-extracted' and taste sour and thin. Shots that take longer than 30 seconds are 'over-extracted' and taste bitter and harsh.</p>
<h3>Variables to Control</h3>
<ul>
<li><strong>Grind Size:</strong> Fine but not powdery; consistency is key.</li>
<li><strong>Tamping Pressure:</strong> Use about 30 pounds of force for a level tamp.</li>
<li><strong>Water Temperature:</strong> 200-205°F (93-96°C) is optimal.</li>
<li><strong>Coffee-to-Water Ratio:</strong> Standard is 1:2.5 (18g coffee to 45g liquid).</li>
</ul>
<p>Once you master these fundamentals, you'll be able to diagnose and fix any espresso problem that comes your way.</p>",
                coverImageUrl: "https://images.unsplash.com/photo-1559056199-641a0ac8b3f4?w=800",
                coverImageFileName: null,
                seoTitle: "Complete Guide to Espresso Extraction | Morii Coffee",
                seoDescription: "Learn the science behind perfect espresso. Master timing, pressure, and grind size for consistent, delicious shots.",
                isFeatured: true,
                displayOrder: 1,
                status: EBlogPostStatus.Published),

            BlogPost.Create(
                title: "Single Origin vs Blend: What's the Difference?",
                slug: "single-origin-vs-blend",
                excerpt: "Explore the distinct characteristics of single-origin and blended coffees, and learn which is right for your palate.",
                contentJson: null,
                contentHtml: @"<h2>Single Origin Coffee</h2>
<p>Single-origin coffee comes from a specific region, farm, or even a particular lot within a farm. This traceability allows roasters and coffee enthusiasts to explore the unique terroir—the environmental factors that influence flavor.</p>
<h3>Characteristics of Single-Origin Coffees</h3>
<ul>
<li>Distinctive regional flavor profiles (e.g., Ethiopian naturals have fruity notes)</li>
<li>Greater complexity and nuance in flavor</li>
<li>Seasonal variations as harvests differ year to year</li>
<li>Higher price point due to exclusivity</li>
</ul>
<h2>Blended Coffee</h2>
<p>Blends combine coffees from multiple origins to create a balanced, consistent profile. A skilled blender curates specific ratios to achieve desired flavor goals.</p>
<h3>Characteristics of Blends</h3>
<ul>
<li>Balanced, approachable flavor profiles</li>
<li>Consistent taste throughout the year</li>
<li>Often more affordable than single-origins</li>
<li>Typically well-suited to espresso applications</li>
</ul>
<p>Both have their place in the coffee world. Single-origins are perfect for those seeking adventure and discovery, while blends provide reliable, delicious consistency.</p>",
                coverImageUrl: "https://images.unsplash.com/photo-1559390566-a4f58da3e7d8?w=800",
                coverImageFileName: null,
                seoTitle: "Single Origin vs Blended Coffee | Morii Coffee",
                seoDescription: "Understand the difference between single-origin and blended coffees. Which profile suits your taste?",
                isFeatured: true,
                displayOrder: 2,
                status: EBlogPostStatus.Published),

            // Recipes & Ideas posts
            BlogPost.Create(
                title: "5 Viral Coffee Recipes to Make at Home",
                slug: "viral-coffee-recipes",
                excerpt: "Try trending coffee drinks that took social media by storm. From cold foam lattes to honey cinnamon coffee, we've got the recipes.",
                contentJson: null,
                contentHtml: @"<h2>1. Cold Foam Latte</h2>
<p>Whip up cold milk with a hand frother until it's airy and bubbly. Pour over a shot of espresso and cold milk for an Instagram-worthy drink.</p>
<h2>2. Honey Cinnamon Cold Brew</h2>
<p>Combine cold brew, a drizzle of honey, a pinch of cinnamon, and oat milk. The warmth of cinnamon against the refreshing cold brew is unmatched.</p>
<h2>3. Affogato with Vanilla Ice Cream</h2>
<p>Pour a hot shot of espresso over vanilla ice cream. Simple, elegant, and pure joy in a cup.</p>
<h2>4. Dalgona Coffee (Whipped Coffee)</h2>
<p>Whip instant coffee, sugar, and hot water until fluffy. Spoon over cold milk for a trendy, frothy drink.</p>
<h2>5. Cardamom Spiced Latte</h2>
<p>Add a pinch of ground cardamom to steamed milk and espresso. An aromatic twist on the classic latte.</p>",
                coverImageUrl: "https://images.unsplash.com/photo-1517668808822-9ebb02ae2a0e?w=800",
                coverImageFileName: null,
                seoTitle: "5 Popular Coffee Recipes to Try at Home | Morii Coffee",
                seoDescription: "Learn how to make viral coffee drinks at home. Easy recipes for cold foam lattes, affogatos, and more.",
                isFeatured: true,
                displayOrder: 3,
                status: EBlogPostStatus.Published),

            BlogPost.Create(
                title: "The Science of Latte Art: Getting Your Milk Temperature Right",
                slug: "latte-art-guide",
                excerpt: "Perfect microfoam and milk temperature are the secrets to café-quality latte art. Learn the science behind the pour.",
                contentJson: null,
                contentHtml: @"<h2>Why Temperature Matters</h2>
<p>The ideal milk temperature for steaming is 150-155°F (65-68°C). Too cold and the milk won't stretch properly. Too hot and you risk burning it and losing its sweetness.</p>
<h3>The Perfect Microfoam</h3>
<p>Microfoam is tiny, uniform bubbles that integrate seamlessly with steamed milk. This is what makes latte art possible.</p>
<h3>How to Steam Milk</h3>
<ol>
<li>Fill a pitcher one-third with cold milk.</li>
<li>Insert the steam wand just below the surface.</li>
<li>Tilt the pitcher at a 15-degree angle and move it slightly.</li>
<li>When the milk reaches 150°F, angle the wand deeper for 10 more seconds.</li>
<li>Remove and tap the pitcher to burst large bubbles.</li>
</ol>
<p>With practice, you'll create beautiful, creamy latte art every time.</p>",
                coverImageUrl: "https://images.unsplash.com/photo-1509785307050-d4066910ec1e?w=800",
                coverImageFileName: null,
                seoTitle: "Master Latte Art: Milk Steaming Guide | Morii Coffee",
                seoDescription: "Learn the temperature and technique secrets for perfect microfoam and café-quality latte art.",
                isFeatured: false,
                displayOrder: 4,
                status: EBlogPostStatus.Published),

            // Sustainability posts
            BlogPost.Create(
                title: "Our Journey to 100% Ethically Sourced Coffee",
                slug: "ethical-sourcing-commitment",
                excerpt: "Discover how Morii Coffee partners directly with farmers to ensure fair wages and sustainable practices.",
                contentJson: null,
                contentHtml: @"<h2>What Does Ethical Sourcing Mean?</h2>
<p>Ethical sourcing ensures that farmers receive fair compensation for their crops, work in safe conditions, and use sustainable farming practices. At Morii Coffee, this is not just a buzzword—it's foundational to our values.</p>
<h2>Our Direct Trade Model</h2>
<p>Rather than buying through middlemen, we purchase directly from coffee farms we've personally visited. This relationship ensures:</p>
<ul>
<li>Farmers receive 20-30% more than commodity market prices</li>
<li>Long-term contracts provide stability and planning security</li>
<li>We can verify sustainable and fair-labor practices firsthand</li>
</ul>
<h2>Environmental Responsibility</h2>
<p>We prioritize farms that use shade-grown methods, which preserve biodiversity and prevent soil degradation. Many of our partners have moved toward fully organic certification.</p>
<h2>Our Commitment Moving Forward</h2>
<p>By 2026, 100% of Morii Coffee beans will be from ethically verified sources. Every cup you drink supports a farmer who is paid fairly and farms responsibly.</p>",
                coverImageUrl: "https://images.unsplash.com/photo-1559056199-641a0ac8b3f4?w=800",
                coverImageFileName: null,
                seoTitle: "Ethical Coffee Sourcing | Morii Coffee Commitment",
                seoDescription: "Learn how Morii Coffee ensures fair wages, sustainable practices, and direct relationships with coffee farmers worldwide.",
                isFeatured: true,
                displayOrder: 5,
                status: EBlogPostStatus.Published),

            BlogPost.Create(
                title: "Reducing Waste: Our Reusable Cup Program",
                slug: "reusable-cup-program",
                excerpt: "Join our mission to eliminate single-use cups. Learn about our incentive program and environmental impact.",
                contentJson: null,
                contentHtml: @"<h2>The Problem with Disposable Cups</h2>
<p>Over 16 billion disposable coffee cups are used annually worldwide. While paper cups are recyclable, the polyethylene coating often prevents them from being processed in standard recycling facilities.</p>
<h2>The Morii Reusable Cup Program</h2>
<p>Starting this month, we're rolling out our reusable cup initiative:</p>
<ul>
<li>Bring your own cup and get 10,000 VND off your drink</li>
<li>Or purchase our branded ceramic mug for 450,000 VND and get unlimited 10% discounts</li>
<li>All cups are dishwasher safe and built to last years</li>
</ul>
<h2>Our Goals</h2>
<p>We aim to reduce disposable cup usage by 50% within 12 months. If 10,000 customers use reusable cups instead of disposables, we'll prevent 30,000+ cups from entering landfills annually.</p>
<h2>Join Us</h2>
<p>Every cup counts. Together, we can make a real difference for our planet.</p>",
                coverImageUrl: "https://images.unsplash.com/photo-1577720643272-265f434b3eab?w=800",
                coverImageFileName: null,
                seoTitle: "Reusable Cup Program | Morii Coffee Sustainability",
                seoDescription: "Learn about Morii's reusable cup initiative. Reduce waste, save money, and help the environment.",
                isFeatured: false,
                displayOrder: 6,
                status: EBlogPostStatus.Published),

            // Cafe News posts
            BlogPost.Create(
                title: "Grand Opening: Morii Coffee District 1 Location",
                slug: "district-1-grand-opening",
                excerpt: "We're thrilled to announce the opening of our newest café in the heart of Ho Chi Minh City's District 1. Join us for the opening week festivities!",
                contentJson: null,
                contentHtml: @"<h2>Welcome to Morii Coffee District 1</h2>
<p>After months of careful planning, we're delighted to open our newest location at 123 Dong Khoi Street, District 1, Ho Chi Minh City. This 100-square-meter space combines modern design with a cozy, welcoming atmosphere.</p>
<h2>Grand Opening Week Events</h2>
<ul>
<li><strong>May 21-23:</strong> Free espresso shots with any drink purchase</li>
<li><strong>May 24:</strong> Live music from local barista-musicians (6 PM - 9 PM)</li>
<li><strong>May 25:</strong> Coffee tasting masterclass with our head roaster (2 PM)</li>
</ul>
<h2>What to Expect</h2>
<p>Our new café features a professional espresso bar, comfortable seating for 40 guests, complimentary WiFi, and a curated selection of coffee equipment and beans for purchase.</p>
<h2>Opening Hours</h2>
<p>Monday - Friday: 7 AM - 8 PM<br>
Saturday - Sunday: 8 AM - 9 PM</p>
<p>We can't wait to serve you!</p>",
                coverImageUrl: "https://images.unsplash.com/photo-1559056199-641a0ac8b3f4?w=800",
                coverImageFileName: null,
                seoTitle: "District 1 Café Grand Opening | Morii Coffee",
                seoDescription: "Celebrate the opening of Morii Coffee's new District 1 location with special events and exclusive offers.",
                isFeatured: true,
                displayOrder: 7,
                status: EBlogPostStatus.Published),

            BlogPost.Create(
                title: "Summer Seasonal Blends Are Here",
                slug: "summer-seasonal-blends",
                excerpt: "Experience our new limited-edition summer blends designed to refresh and invigorate. Now available for a limited time.",
                contentJson: null,
                contentHtml: @"<h2>Summer 2026 Limited Editions</h2>
<p>As temperatures rise, so does the excitement for our new seasonal blends. This summer, we're introducing three exclusive coffees crafted to brighten your season:</p>
<h3>Tropical Paradise Blend</h3>
<p>A vibrant blend of Ethiopian Yirgacheffe and Kenyan AA beans. Notes of tropical fruit, jasmine, and a crisp finish. Perfect iced.</p>
<h3>Morning Sunrise</h3>
<p>Colombian Geisha meets Brazilian natural process. Floral aromatics with hints of honey and citrus. Great for morning clarity.</p>
<h3>Sunset Twilight</h3>
<p>An intriguing blend of Indonesian and Central American origins. Dark chocolate, earthy undertones, and a smooth, velvety body.</p>
<h2>Availability</h2>
<p>These blends are available in-café and online from June 1st - August 31st. Limited quantities, so grab yours while supplies last!</p>
<h2>Special Offer</h2>
<p>Purchase any 500g bag of seasonal blend this week and get a 200g complementary bag of your choice.</p>",
                coverImageUrl: "https://images.unsplash.com/photo-1559390566-a4f58da3e7d8?w=800",
                coverImageFileName: null,
                seoTitle: "Summer Seasonal Coffee Blends | Morii Coffee 2026",
                seoDescription: "Explore Morii's new summer seasonal blends. Limited edition coffee with tropical, floral, and rich chocolate notes.",
                isFeatured: true,
                displayOrder: 8,
                status: EBlogPostStatus.Published),
        };
    }

    private static List<BlogPostCategory> GetSeedBlogPostCategories(
        List<BlogPost> posts, List<BlogCategory> categories)
    {
        var catBySlug = categories.ToDictionary(c => c.Slug);
        var result = new List<BlogPostCategory>();

        // Map each post slug to its category
        var slugToCategories = new Dictionary<string, string[]>
        {
            { "espresso-extraction-guide", new[] { "coffee-education" } },
            { "single-origin-vs-blend", new[] { "coffee-education" } },
            { "viral-coffee-recipes", new[] { "recipes-ideas" } },
            { "latte-art-guide", new[] { "recipes-ideas" } },
            { "ethical-sourcing-commitment", new[] { "sustainability" } },
            { "reusable-cup-program", new[] { "sustainability" } },
            { "district-1-grand-opening", new[] { "cafe-news" } },
            { "summer-seasonal-blends", new[] { "cafe-news" } },
        };

        foreach (var post in posts)
        {
            if (!slugToCategories.TryGetValue(post.Slug, out var categoryNames))
                continue;

            foreach (var categoryName in categoryNames)
            {
                if (!catBySlug.TryGetValue(categoryName, out var category))
                    continue;

                result.Add(new BlogPostCategory
                {
                    BlogPostId = post.Id,
                    BlogCategoryId = category.Id,
                });
            }
        }

        return result;
    }
}
