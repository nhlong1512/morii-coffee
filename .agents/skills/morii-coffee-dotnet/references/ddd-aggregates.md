# DDD Aggregates & Persistence — MoriiCoffee

## Aggregate Root Base Class

```csharp
// Domain/SeedWork/AggregateRoot.cs
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void RaiseDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    public void ClearDomainEvents() => _domainEvents.Clear();
}

// Domain/SeedWork/Entity.cs
public abstract class Entity
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
}
```

---

## Aggregate Structure Example

```
Domain/Aggregates/ProductAggregate/
├── Product.cs                    ← Aggregate Root
├── Entities/
│   ├── ProductVariant.cs         ← Child entity (has ID, owned by Product)
│   └── ProductImage.cs           ← Child entity
├── ValueObjects/
│   └── ProductCategory.cs        ← Join table / value-by-composition
└── Events/
    └── ProductCreatedDomainEvent.cs
```

### Aggregate Root
```csharp
public class Product : AggregateRoot
{
    // Private setters — only the aggregate mutates its own state
    public string Name { get; private set; }
    public string Slug { get; private set; }
    public string Description { get; private set; }
    public EProductStatus Status { get; private set; }
    public bool IsFeatured { get; private set; }

    // Owned collections
    private readonly List<ProductVariant> _variants = [];
    private readonly List<ProductImage> _images = [];
    private readonly List<ProductCategory> _productCategories = [];

    public IReadOnlyCollection<ProductVariant> Variants => _variants.AsReadOnly();
    public IReadOnlyCollection<ProductImage> Images => _images.AsReadOnly();
    public IReadOnlyCollection<ProductCategory> ProductCategories => _productCategories.AsReadOnly();

    // EF Core needs a parameterless constructor
    protected Product() { }

    public Product(string name, string description)
    {
        Name = name;
        Slug = GenerateSlug(name);
        Description = description;
        Status = EProductStatus.Active;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new ProductCreatedDomainEvent(Id));
    }

    // Domain methods enforce invariants
    public void UpdateDetails(string name, string description)
    {
        Name = name;
        Slug = GenerateSlug(name);
        Description = description;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SoftDelete()
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        Status = EProductStatus.Inactive;
    }

    private static string GenerateSlug(string name) =>
        name.ToLower().Replace(" ", "-");
}
```

### Child Entity
```csharp
public class ProductVariant : Entity
{
    public Guid ProductId { get; private set; }
    public EProductSize Size { get; private set; }
    public decimal BasePrice { get; private set; }
    public decimal AdditionalPrice { get; private set; }
    public bool IsDefault { get; private set; }
    public int Stock { get; private set; } // -1 = unlimited

    public decimal TotalPrice => BasePrice + AdditionalPrice;

    protected ProductVariant() { }

    public ProductVariant(Guid productId, EProductSize size, decimal basePrice,
        decimal additionalPrice, bool isDefault, int stock = -1)
    {
        ProductId = productId;
        Size = size;
        BasePrice = basePrice;
        AdditionalPrice = additionalPrice;
        IsDefault = isDefault;
        Stock = stock;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }
}
```

### Value Object (Join Table)
```csharp
// Has no independent identity — only meaningful in context of its aggregate
public class ProductCategory
{
    public Guid ProductId { get; private set; }
    public Guid CategoryId { get; private set; }

    public Product Product { get; private set; } = null!;
    public Category Category { get; private set; } = null!;

    protected ProductCategory() { }

    public ProductCategory(Guid productId, Guid categoryId)
    {
        ProductId = productId;
        CategoryId = categoryId;
    }
}
```

---

## Repository Interface (Domain layer)

```csharp
// Domain/Repositories/IProductsRepository.cs
public interface IProductsRepository : IRepositoryBase<Product>
{
    Task<Product?> GetBySlugAsync(string slug);
    Task<bool> SlugExistsAsync(string slug);
    Task<Pagination<Product>> GetPaginatedAsync(ProductPaginationFilter filter);
}

// Domain/SeedWork/IRepositoryBase.cs
public interface IRepositoryBase<T> where T : AggregateRoot
{
    Task<T?> GetByIdAsync(Guid id);
    Task CreateAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);  // sets IsDeleted = true
}

// Domain/SeedWork/IUnitOfWork.cs
public interface IUnitOfWork
{
    IProductsRepository Products { get; }
    IProductVariantsRepository ProductVariants { get; }
    ICategoriesRepository Categories { get; }
    // ... all other repositories
    Task CommitAsync();
}
```

---

## EF Core Configuration (Infrastructure.Persistence)

```csharp
// Configurations/ProductConfiguration.cs
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Slug).IsRequired().HasMaxLength(250);
        builder.HasIndex(p => p.Slug).IsUnique();
        builder.Property(p => p.Status).HasConversion<int>();

        // Soft delete global query filter
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Relationships
        builder.HasMany(p => p.Variants)
            .WithOne()
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

// Configurations/ProductCategoryConfiguration.cs
public class ProductCategoryConfiguration : IEntityTypeConfiguration<ProductCategory>
{
    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.HasKey(pc => new { pc.ProductId, pc.CategoryId });  // composite PK

        builder.HasOne(pc => pc.Product)
            .WithMany(p => p.ProductCategories)
            .HasForeignKey(pc => pc.ProductId);

        builder.HasOne(pc => pc.Category)
            .WithMany(c => c.ProductCategories)
            .HasForeignKey(pc => pc.CategoryId);
    }
}
```

---

## Concrete Repository (Infrastructure.Persistence)

```csharp
// Repositories/ProductsRepository.cs
public class ProductsRepository : RepositoryBase<Product>, IProductsRepository
{
    public ProductsRepository(ApplicationDbContext context) : base(context) { }

    public async Task<Product?> GetBySlugAsync(string slug) =>
        await _context.Products
            .Include(p => p.Variants)
            .Include(p => p.ProductCategories).ThenInclude(pc => pc.Category)
            .FirstOrDefaultAsync(p => p.Slug == slug);

    public async Task<bool> SlugExistsAsync(string slug) =>
        await _context.Products.AnyAsync(p => p.Slug == slug);

    public async Task<Pagination<Product>> GetPaginatedAsync(ProductPaginationFilter filter)
    {
        var query = _context.Products.AsQueryable();

        if (filter.CategoryId.HasValue)
            query = query.Where(p => p.ProductCategories
                .Any(pc => pc.CategoryId == filter.CategoryId));

        if (filter.IsFeatured.HasValue)
            query = query.Where(p => p.IsFeatured == filter.IsFeatured);

        var total = await query.CountAsync();
        var items = await query
            .Skip((filter.PageNumber - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new Pagination<Product>(items, total, filter.PageNumber, filter.PageSize);
    }
}
```

---

## Soft Delete & Restore Commands Convention

- `DeleteProductCommand` → sets `IsDeleted = true`, `DeletedAt = now`
- `RestoreProductCommand` → sets `IsDeleted = false`, `DeletedAt = null`
- `PermanentDeleteProductCommand` → actually removes the row (use with caution)

The global EF Core query filter `HasQueryFilter(x => !x.IsDeleted)` hides soft-deleted records automatically. To query deleted records, use `IgnoreQueryFilters()`.
