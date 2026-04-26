using System.Linq.Expressions;
using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Domain.SeedWork.Persistence;

/// <summary>
/// Defines the generic repository contract for entities of type <typeparamref name="T"/>.
/// Supports CRUD operations, soft deletion, restoration, pagination, and eager loading.
/// </summary>
public interface IRepositoryBase<T> where T : EntityBase
{
    #region Query Methods

    /// <summary>Returns a queryable of all non-deleted entities (optional change tracking).</summary>
    IQueryable<T> FindAll(bool trackChanges = false);

    /// <summary>Returns a queryable of all non-deleted entities, including specified navigation properties.</summary>
    IQueryable<T> FindAll(bool trackChanges = false, params Expression<Func<T, object>>[] includeProperties);

    /// <summary>Returns a filtered queryable of non-deleted entities matching the given expression.</summary>
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false);

    /// <summary>Returns a filtered queryable including specified navigation properties.</summary>
    IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false,
        params Expression<Func<T, object>>[] includeProperties);

    /// <summary>Returns a paginated result of all non-deleted entities.</summary>
    Pagination<T> PaginatedFind(PaginationFilter filter, bool trackChanges = false);

    /// <summary>Returns a paginated result with custom include paths.</summary>
    Pagination<T> PaginatedFind(PaginationFilter filter,
        Func<IQueryable<T>, IQueryable<T>> includePaths, bool trackChanges = false);

    /// <summary>Returns a paginated result of entities matching the given expression.</summary>
    Pagination<T> PaginatedFindByCondition(Expression<Func<T, bool>> expression, PaginationFilter filter,
        bool trackChanges = false);

    /// <summary>Returns a paginated result of entities matching the expression with include paths.</summary>
    Pagination<T> PaginatedFindByCondition(Expression<Func<T, bool>> expression, PaginationFilter filter,
        Func<IQueryable<T>, IQueryable<T>> includePaths, bool trackChanges = false);

    #endregion

    #region Existence Methods

    /// <summary>Checks whether a non-deleted entity with the given ID exists.</summary>
    Task<bool> ExistAsync(Guid id);

    /// <summary>Checks whether any non-deleted entity satisfies the given expression.</summary>
    Task<bool> ExistAsync(Expression<Func<T, bool>> expression);

    #endregion

    #region Counting Methods

    /// <summary>Returns the total count of entities (including deleted).</summary>
    int Count();

    /// <summary>Asynchronously returns the total count of entities.</summary>
    Task<int> CountAsync();

    #endregion

    #region Retrieval Methods

    /// <summary>Retrieves a non-deleted entity by its primary key.</summary>
    Task<T?> GetByIdAsync(Guid id);

    /// <summary>Retrieves a non-deleted entity by its primary key, including specified navigation properties.</summary>
    Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includeProperties);

    #endregion

    #region Creation Methods

    /// <summary>Adds a new entity to the context (persisted on CommitAsync).</summary>
    Task CreateAsync(T entity);

    /// <summary>Adds a list of new entities to the context.</summary>
    Task CreateListAsync(IEnumerable<T> entities);

    #endregion

    #region Update Methods

    /// <summary>Updates the given entity in the context.</summary>
    Task Update(T entity);

    #endregion

    #region Deletion Methods

    /// <summary>Physically removes the entity from the database.</summary>
    Task Delete(T entity);

    /// <summary>Physically removes a collection of entities from the database.</summary>
    Task DeleteList(IEnumerable<T> entities);

    #endregion

    #region Soft Delete & Restore

    /// <summary>Marks the entity as deleted without removing it from the database.</summary>
    Task SoftDelete(T entity);

    /// <summary>Restores a previously soft-deleted entity.</summary>
    Task Restore(T entity);

    #endregion
}
