using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Repository contract for managing <see cref="UserDeliveryProfile"/> records.
/// Does not extend <see cref="MoriiCoffee.Domain.SeedWork.Persistence.IRepositoryBase{T}"/> because the
/// entity's primary key is <c>UserId</c> (not <c>Id</c>), making the generic base incompatible.
/// </summary>
public interface IUserDeliveryProfileRepository
{
    /// <summary>
    /// Returns the delivery profile for <paramref name="userId"/>,
    /// or <c>null</c> if none has been saved yet.
    /// </summary>
    Task<UserDeliveryProfile?> GetByUserIdAsync(Guid userId);

    /// <summary>
    /// Inserts or updates the delivery profile for a user (upsert).
    /// If a profile already exists it is updated; otherwise a new row is inserted.
    /// </summary>
    Task UpsertAsync(UserDeliveryProfile profile);
}
