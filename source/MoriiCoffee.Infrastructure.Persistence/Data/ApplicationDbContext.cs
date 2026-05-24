using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.StoreAggregate;
using MoriiCoffee.Domain.Aggregates.StoreAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Aggregates.WishlistAggregate;

namespace MoriiCoffee.Infrastructure.Persistence.Data;

/// <summary>
/// The main EF Core database context for MoriiCoffee.
/// Extends IdentityDbContext to provide Identity tables (AspNetUsers, AspNetRoles, etc.).
/// Applies all entity configurations from the current assembly automatically.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<User, Role, Guid>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // --- Identity ---
    #region UserAggregate

    public override DbSet<User> Users { get; set; }
    public override DbSet<Role> Roles { get; set; }
    public DbSet<UserDeliveryProfile> UserDeliveryProfiles { get; set; }

    #endregion

    // --- Catalog ---
    #region CategoryAggregate

    public DbSet<Category> Categories { get; set; }

    #endregion

    #region BannerAggregate

    public DbSet<Banner> Banners { get; set; }

    #endregion

    #region BlogAggregate

    public DbSet<BlogPost> BlogPosts { get; set; }
    public DbSet<BlogCategory> BlogCategories { get; set; }
    public DbSet<BlogPostCategory> BlogPostCategories { get; set; }

    #endregion

    #region ProductAggregate

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }

    #endregion

    #region OrderAggregate

    public DbSet<Order> Orders { get; set; }
    public DbSet<OrderItem> OrderItems { get; set; }
    public DbSet<ShippingProvince> ShippingProvinces { get; set; }
    public DbSet<ShippingDistrict> ShippingDistricts { get; set; }
    public DbSet<ShippingWard> ShippingWards { get; set; }
    public DbSet<Shipment> Shipments { get; set; }
    public DbSet<ShipmentWebhookEvent> ShipmentWebhookEvents { get; set; }

    #endregion

    #region PaymentAggregate

    public DbSet<Payment> Payments { get; set; }
    public DbSet<RefundRecord> Refunds { get; set; }
    public DbSet<PaymentWebhookEvent> PaymentWebhookEvents { get; set; }

    #endregion

    #region WishlistAggregate

    public DbSet<WishlistItem> WishlistItems { get; set; }

    #endregion

    #region StoreAggregate

    /// <summary>Store locations available on the public store locator.</summary>
    public DbSet<Store> Stores { get; set; }

    /// <summary>Opening hours records for each store (7 per store, one per day of week).</summary>
    public DbSet<StoreOpeningHours> StoreOpeningHours { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // MUST call base first so Identity creates its tables
        base.OnModelCreating(builder);

        // Auto-apply all IEntityTypeConfiguration<T> implementations in this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
