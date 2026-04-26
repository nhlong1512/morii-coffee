using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.SeedWork.Entities;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;
using MoriiCoffee.Infrastructure.Persistence.Data;

namespace MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

/// <summary>
/// Generic EF Core repository base providing all standard CRUD,
/// pagination, and soft-delete operations for <typeparamref name="T"/>.
/// </summary>
public class RepositoryBase<T> : IRepositoryBase<T> where T : EntityBase
{
    private readonly ApplicationDbContext _context;

    public RepositoryBase(ApplicationDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public IQueryable<T> FindAll(bool trackChanges = false)
    {
        return !trackChanges
            ? _context.Set<T>().AsNoTracking().Where(e => e.DeletedAt == null)
            : _context.Set<T>().Where(e => e.DeletedAt == null);
    }

    public IQueryable<T> FindAll(bool trackChanges = false,
        params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> items = FindAll(trackChanges);
        return includeProperties.Aggregate(items, (current, include) => current.Include(include));
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false)
    {
        return !trackChanges
            ? _context.Set<T>().Where(e => e.DeletedAt == null).Where(expression).AsNoTracking()
            : _context.Set<T>().Where(e => e.DeletedAt == null).Where(expression);
    }

    public IQueryable<T> FindByCondition(Expression<Func<T, bool>> expression, bool trackChanges = false,
        params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> items = FindByCondition(expression, trackChanges);
        return includeProperties.Aggregate(items, (current, include) => current.Include(include));
    }

    public Pagination<T> PaginatedFind(PaginationFilter filter, bool trackChanges = false)
    {
        return PagingHelper.QueryPaginate(filter, FindAll(trackChanges));
    }

    public Pagination<T> PaginatedFind(PaginationFilter filter,
        Func<IQueryable<T>, IQueryable<T>> includePaths, bool trackChanges = false)
    {
        return PagingHelper.QueryPaginate(filter, includePaths(FindAll(trackChanges)));
    }

    public Pagination<T> PaginatedFindByCondition(Expression<Func<T, bool>> expression,
        PaginationFilter filter, bool trackChanges = false)
    {
        return PagingHelper.QueryPaginate(filter, FindByCondition(expression, trackChanges));
    }

    public Pagination<T> PaginatedFindByCondition(Expression<Func<T, bool>> expression,
        PaginationFilter filter, Func<IQueryable<T>, IQueryable<T>> includePaths,
        bool trackChanges = false)
    {
        return PagingHelper.QueryPaginate(filter, includePaths(FindByCondition(expression, trackChanges)));
    }

    public async Task<bool> ExistAsync(Guid id)
    {
        return await _context.Set<T>()
            .AnyAsync(e => EF.Property<Guid>(e, "Id") == id && !e.IsDeleted);
    }

    public async Task<bool> ExistAsync(Expression<Func<T, bool>> expression)
    {
        return await _context.Set<T>().Where(e => !e.IsDeleted).AnyAsync(expression);
    }

    public int Count() => _context.Set<T>().Count();

    public Task<int> CountAsync() => _context.Set<T>().CountAsync();

    public async Task<T?> GetByIdAsync(Guid id)
    {
        var entity = await _context.Set<T>().FindAsync(id);
        return entity is not null && !entity.IsDeleted ? entity : null;
    }

    public async Task<T?> GetByIdAsync(Guid id, params Expression<Func<T, object>>[] includeProperties)
    {
        IQueryable<T> query = _context.Set<T>().Where(e => !e.IsDeleted);
        query = includeProperties.Aggregate(query, (current, include) => current.Include(include));
        return await query.FirstOrDefaultAsync(e => EF.Property<Guid>(e, "Id") == id);
    }

    public async Task CreateAsync(T entity) => await _context.Set<T>().AddAsync(entity);

    public async Task CreateListAsync(IEnumerable<T> entities) =>
        await _context.Set<T>().AddRangeAsync(entities);

    public Task Update(T entity)
    {
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task Delete(T entity)
    {
        _context.Set<T>().Remove(entity);
        return Task.CompletedTask;
    }

    public Task DeleteList(IEnumerable<T> entities)
    {
        _context.Set<T>().RemoveRange(entities);
        return Task.CompletedTask;
    }

    public Task SoftDelete(T entity)
    {
        entity.IsDeleted = true;
        entity.DeletedAt = DateTime.UtcNow;
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }

    public Task Restore(T entity)
    {
        entity.IsDeleted = false;
        entity.DeletedAt = null;
        _context.Entry(entity).State = EntityState.Modified;
        return Task.CompletedTask;
    }
}
