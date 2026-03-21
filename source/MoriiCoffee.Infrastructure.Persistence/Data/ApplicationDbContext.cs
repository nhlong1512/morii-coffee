using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.BannerAggregate;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;

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

    #endregion

    // --- Catalog ---
    #region CategoryAggregate

    public DbSet<Category> Categories { get; set; }

    #endregion

    #region BannerAggregate

    public DbSet<Banner> Banners { get; set; }

    #endregion

    #region ProductAggregate

    public DbSet<Product> Products { get; set; }
    public DbSet<ProductVariant> ProductVariants { get; set; }
    public DbSet<ProductImage> ProductImages { get; set; }
    public DbSet<ProductCategory> ProductCategories { get; set; }

    #endregion

    protected override void OnModelCreating(ModelBuilder builder)
    {
        // MUST call base first so Identity creates its tables
        base.OnModelCreating(builder);

        // Auto-apply all IEntityTypeConfiguration<T> implementations in this assembly
        builder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
    }
}
