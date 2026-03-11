using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.User.GetUserById;

/// <summary>Query to retrieve a user's full profile by their ID. Throws NotFoundException if the user does not exist.</summary>
public class GetUserByIdQuery : IQuery<UserDto>
{
    public Guid UserId { get; set; }

    public GetUserByIdQuery(Guid userId) => UserId = userId;
}
