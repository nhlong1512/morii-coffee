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
    /// with <c>SqlServerRetryingExecutionStrategy</c>. Commits on success, rolls back on failure.
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

    #endregion
}
