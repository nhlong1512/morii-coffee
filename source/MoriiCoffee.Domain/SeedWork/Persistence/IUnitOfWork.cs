using MoriiCoffee.Domain.Repositories;

namespace MoriiCoffee.Domain.SeedWork.Persistence;

/// <summary>
/// Coordinates all repository operations and manages database transactions.
/// Follows the Unit of Work pattern to ensure consistency across multiple repository calls.
/// </summary>
public interface IUnitOfWork : IDisposable
{
    /// <summary>Persists all pending changes to the database.</summary>
    Task<int> CommitAsync();

    /// <summary>Begins a new database transaction.</summary>
    Task BeginTransactionAsync();

    /// <summary>Commits the current database transaction.</summary>
    Task EndTransactionAsync();

    /// <summary>Rolls back the current database transaction.</summary>
    Task RollbackTransactionAsync();

    /// <summary>
    /// Executes <paramref name="operation"/> inside a retriable transaction that is compatible
    /// with the provider's retrying execution strategy. Commits on success, rolls back on failure.
    /// </summary>
    Task ExecuteInTransactionAsync(Func<Task> operation);

    #region Repositories

    /// <summary>Repository for managing product categories.</summary>
    ICategoriesRepository Categories { get; }

    /// <summary>Repository for managing products.</summary>
    IProductsRepository Products { get; }

    /// <summary>Repository for managing product variants (sizes/options).</summary>
    IProductVariantsRepository ProductVariants { get; }

    /// <summary>Repository for managing product gallery images.</summary>
    IProductImagesRepository ProductImages { get; }

    /// <summary>Repository for managing promotional banners.</summary>
    IBannersRepository Banners { get; }

    /// <summary>Repository for managing blog posts.</summary>
    IBlogPostsRepository BlogPosts { get; }

    /// <summary>Repository for managing blog categories.</summary>
    IBlogCategoriesRepository BlogCategories { get; }

    /// <summary>Repository for managing customer orders.</summary>
    IOrderRepository Orders { get; }

    /// <summary>Repository for managing user delivery profiles.</summary>
    IUserDeliveryProfileRepository UserDeliveryProfiles { get; }

    /// <summary>Repository for the Payment aggregate (Stripe checkout sessions + refunds).</summary>
    IPaymentRepository Payments { get; }

    /// <summary>Repository for the PaymentWebhookEvent audit/idempotency table.</summary>
    IPaymentWebhookEventRepository PaymentWebhookEvents { get; }

    /// <summary>Read-only repository for admin reporting aggregates.</summary>
    IAdminReportsReadRepository AdminReports { get; }

    /// <summary>Repository for managing user wishlist items.</summary>
    IWishlistItemRepository WishlistItems { get; }

    /// <summary>Repository for managing store locations.</summary>
    IStoresRepository Stores { get; }

    /// <summary>Repository for reading cached GHN master data.</summary>
    IShippingMasterDataRepository ShippingMasterData { get; }

    /// <summary>Repository for normalized shipments linked to orders.</summary>
    IShipmentRepository Shipments { get; }

    /// <summary>Repository for GHN webhook audit/idempotency rows.</summary>
    IShipmentWebhookEventRepository ShipmentWebhookEvents { get; }

    #endregion
}
