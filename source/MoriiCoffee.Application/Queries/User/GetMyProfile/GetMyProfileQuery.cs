using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.User.GetMyProfile;

/// <summary>Query to retrieve the full profile of the currently authenticated user, including their assigned roles.</summary>
public class GetMyProfileQuery : IQuery<UserDto>
{
    public Guid UserId { get; set; }

    public GetMyProfileQuery(Guid userId) => UserId = userId;
}
