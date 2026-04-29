using System.Linq.Expressions;
using System.Reflection;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.SeedWork;
using MoriiCoffee.Infrastructure.Persistence.Data;

namespace MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

/// <summary>
/// Decorates a repository with Redis-backed caching for entity-by-id lookups and cache invalidation on writes.
/// </summary>
public class CachedRepositoryBase<T> : ICachedRepositoryBase<T> where T : EntityBase
{
    private static readonly PropertyInfo? IdProperty = typeof(T).GetProperty("Id");

    private readonly IRepositoryBase<T> _decorated;
    private readonly ICacheService _cacheService;

    public CachedRepositoryBase(
        ApplicationDbContext context,
        IRepositoryBase<T> decorated,
        ICacheService cacheService)
    {
        ArgumentNullException.ThrowIfNull(context);
        _decorated = decorated;
        _cacheService = cacheService;
    }

    public IQueryable<T> FindAll(bool trackChanges = false)
    {
        return _decorated.FindAll(trackChanges);
    }

    public IQueryable<T> FindAll(bool trackChanges = false, params Expression<Func<T, object>>[] includeProperties)
    {
        return _decorated.FindAll(trackChanges, includeProperties);
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false)
    {
        return _decorated.FindByCondition(expression, trackChanges);
    }

    public IQueryable<T> FindByCondition(
        Expression<Func<T, bool>> expression,
        bool trackChanges = false,
        params Expression<Func<T, object>>[] includeProperties)
    {
        return _decorated.FindByCondition(expression, trackChanges, includeProperties);
    }

    public Pagination<T> PaginatedFind(PaginationFilter filter, bool trackChanges = false)
    {
        return _decorated.PaginatedFind(filter, trackChanges);
    }

    public Pagination<T> PaginatedFind(
        PaginationFilter filter,
        Func<IQueryable<T>, IQueryable<T>> includePaths,
        bool trackChanges = false)
    {
        return _decorated.PaginatedFind(filter, includePaths, trackChanges);
    }

    public Pagination<T> PaginatedFindByCondition(
        Expression<Func<T, bool>> expression,
        PaginationFilter filter,
        bool trackChanges = false)
    {
        return _decorated.PaginatedFindByCondition(expression, filter, trackChanges);
    }

    public Pagination<T> PaginatedFindByCondition(
        Expression<Func<T, bool>> expression,
        PaginationFilter filter,
        Func<IQueryable<T>, IQueryable<T>> includePaths,
        bool trackChanges = false)
    {
        return _decorated.PaginatedFindByCondition(expression, filter, includePaths, trackChanges);
    }

    public Task<bool> ExistAsync(Guid id)
    {
        return _decorated.ExistAsync(id);
    }

    public Task<bool> ExistAsync(Expression<Func<T, bool>> expression)
    {
        return _decorated.ExistAsync(expression);
    }

    public int Count()
    {
        return _decorated.Count();
    }

    public Task<int> CountAsync()
    {
        return _decorated.CountAsync();
    }

    public async Task<T?> GetByIdAsync(Guid id)
    {
        string cacheKey = GetEntityCacheKey(id);
        T? cachedEntity = await _cacheService.GetDataAsync<T>(cacheKey);
        if (cachedEntity is not null)
        {
            return cachedEntity;
        }

        T? entity = await _decorated.GetByIdAsync(id);
        if (entity is null)
        {
            return null;
        }

        await _cacheService.SetDataAsync(cacheKey, entity, CacheTtlConstants.Default);

        return entity;
    }

    public Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includeProperties)
    {
        return _decorated.GetByIdAsync(id, includeProperties);
    }

    public async Task CreateAsync(T entity)
    {
        await _decorated.CreateAsync(entity);
        await InvalidateCollectionCacheAsync();
        await InvalidateEntityCacheAsync(entity);
    }

    public async Task CreateListAsync(IEnumerable<T> entities)
    {
        await _decorated.CreateListAsync(entities);
        await InvalidateCollectionCacheAsync();
    }

    public async Task Update(T entity)
    {
        await _decorated.Update(entity);
        await InvalidateCollectionCacheAsync();
        await InvalidateEntityCacheAsync(entity);
    }

    public async Task Delete(T entity)
    {
        await _decorated.Delete(entity);
        await InvalidateCollectionCacheAsync();
        await InvalidateEntityCacheAsync(entity);
    }

    public async Task DeleteList(IEnumerable<T> entities)
    {
        List<T> entitiesToDelete = entities as List<T> ?? entities.ToList();

        await _decorated.DeleteList(entitiesToDelete);
        await InvalidateCollectionCacheAsync();

        foreach (T entity in entitiesToDelete)
        {
            await InvalidateEntityCacheAsync(entity);
        }
    }

    public async Task SoftDelete(T entity)
    {
        await _decorated.SoftDelete(entity);
        await InvalidateCollectionCacheAsync();
        await InvalidateEntityCacheAsync(entity);
    }

    public async Task Restore(T entity)
    {
        await _decorated.Restore(entity);
        await InvalidateCollectionCacheAsync();
        await InvalidateEntityCacheAsync(entity);
    }

    private Task InvalidateCollectionCacheAsync()
    {
        return _cacheService.RemoveDataAsync(GetCollectionCacheKey());
    }

    private Task InvalidateEntityCacheAsync(T entity)
    {
        Guid entityId = GetEntityId(entity);
        return _cacheService.RemoveDataAsync(GetEntityCacheKey(entityId));
    }

    private static string GetCollectionCacheKey()
    {
        return CachedKeyConstants.EntityCollection<T>();
    }

    private static string GetEntityCacheKey(Guid id)
    {
        return CachedKeyConstants.EntityById<T>(id);
    }

    private static Guid GetEntityId(T entity)
    {
        if (IdProperty?.GetValue(entity) is Guid id)
        {
            return id;
        }

        throw new InvalidOperationException(
            $"Entity type '{typeof(T).Name}' must expose a Guid Id property to use cached repository behavior.");
    }
}
