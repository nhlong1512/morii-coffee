using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.Repositories;

namespace MoriiCoffee.Infrastructure.Persistence.SeedWork.UnitOfWork;

/// <summary>
/// Coordinates all repositories and manages database transactions.
/// Repositories are initialized lazily on first access.
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private bool _disposed;

    private readonly ApplicationDbContext _context;

    private CategoriesRepository? _categories;
    private ProductsRepository? _products;
    private ProductVariantsRepository? _productVariants;
    private ProductImagesRepository? _productImages;
    private BannersRepository? _banners;
    private BlogPostsRepository? _blogPosts;
    private BlogCategoriesRepository? _blogCategories;
    private OrdersRepository? _orders;
    private UserDeliveryProfilesRepository? _userDeliveryProfiles;
    private PaymentRepository? _payments;
    private PaymentWebhookEventRepository? _paymentWebhookEvents;
    private AdminReportsReadRepository? _adminReports;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public ICategoriesRepository Categories =>
        _categories ??= new CategoriesRepository(_context);

    public IProductsRepository Products =>
        _products ??= new ProductsRepository(_context);

    public IProductVariantsRepository ProductVariants =>
        _productVariants ??= new ProductVariantsRepository(_context);

    public IProductImagesRepository ProductImages =>
        _productImages ??= new ProductImagesRepository(_context);

    public IBannersRepository Banners =>
        _banners ??= new BannersRepository(_context);

    public IBlogPostsRepository BlogPosts =>
        _blogPosts ??= new BlogPostsRepository(_context);

    public IBlogCategoriesRepository BlogCategories =>
        _blogCategories ??= new BlogCategoriesRepository(_context);

    public IOrderRepository Orders =>
        _orders ??= new OrdersRepository(_context);

    public IUserDeliveryProfileRepository UserDeliveryProfiles =>
        _userDeliveryProfiles ??= new UserDeliveryProfilesRepository(_context);

    public IPaymentRepository Payments =>
        _payments ??= new PaymentRepository(_context);

    public IPaymentWebhookEventRepository PaymentWebhookEvents =>
        _paymentWebhookEvents ??= new PaymentWebhookEventRepository(_context);

    public IAdminReportsReadRepository AdminReports =>
        _adminReports ??= new AdminReportsReadRepository(_context);

    public async Task<int> CommitAsync() =>
        await _context.SaveChangesAsync();

    public async Task BeginTransactionAsync() =>
        await _context.Database.BeginTransactionAsync();

    public async Task EndTransactionAsync()
    {
        await CommitAsync();
        await _context.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync() =>
        await _context.Database.RollbackTransactionAsync();

    public async Task ExecuteInTransactionAsync(Func<Task> operation)
    {
        var strategy = _context.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                await operation();
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        });
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _context.Dispose();
        }
        _disposed = true;
    }
}
