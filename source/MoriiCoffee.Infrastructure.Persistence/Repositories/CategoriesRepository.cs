using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.CategoryAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;
using MoriiCoffee.Infrastructure.Persistence.SeedWork.Repository;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

public class CategoriesRepository : RepositoryBase<Category>, ICategoriesRepository
{
    private readonly ApplicationDbContext _context;

    public CategoriesRepository(ApplicationDbContext context) : base(context)
    {
        _context = context;
    }

    public async Task<Category?> GetByNameAsync(string name)
    {
        return await _context.Categories
            .Where(c => !c.IsDeleted)
            .FirstOrDefaultAsync(c => c.Name.ToLower() == name.ToLower());
    }
}
