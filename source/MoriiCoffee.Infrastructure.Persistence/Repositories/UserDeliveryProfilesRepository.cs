using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of <see cref="IUserDeliveryProfileRepository"/>.
/// Uses <c>UserId</c> as the primary key rather than the generic <c>Id</c> convention.
/// </summary>
public class UserDeliveryProfilesRepository : IUserDeliveryProfileRepository
{
    private readonly ApplicationDbContext _context;

    public UserDeliveryProfilesRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<UserDeliveryProfile?> GetByUserIdAsync(Guid userId)
    {
        return await _context.UserDeliveryProfiles
            .FirstOrDefaultAsync(p => p.UserId == userId);
    }

    /// <inheritdoc />
    public async Task UpsertAsync(UserDeliveryProfile profile)
    {
        var existing = await _context.UserDeliveryProfiles
            .FirstOrDefaultAsync(p => p.UserId == profile.UserId);

        if (existing is null)
        {
            await _context.UserDeliveryProfiles.AddAsync(profile);
        }
        else
        {
            existing.Update(profile.FullName, profile.PhoneNumber, profile.Address);
        }
    }
}
