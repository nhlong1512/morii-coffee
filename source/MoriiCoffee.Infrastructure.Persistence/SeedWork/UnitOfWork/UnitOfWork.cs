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
    private BannersRepository? _banners;
    private NotificationsRepository? _notifications;
    private PaymentsRepository? _payments;

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

    public IBannersRepository Banners =>
        _banners ??= new BannersRepository(_context);

    public INotificationsRepository Notifications =>
        _notifications ??= new NotificationsRepository(_context);

    public IPaymentsRepository Payments =>
        _payments ??= new PaymentsRepository(_context);

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
