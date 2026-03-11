using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.User.GetPaginatedUsers;

/// <summary>Query to retrieve a paginated, filterable list of all users. Supports search by email/username/name and filter by status.</summary>
public class GetPaginatedUsersQuery : IQuery<Pagination<UserSummaryDto>>
{
    public PaginationFilter Filter { get; set; }
    public string? Search { get; set; }
    public EUserStatus? Status { get; set; }

    public GetPaginatedUsersQuery(PaginationFilter filter, string? search, EUserStatus? status)
    {
        Filter = filter;
        Search = search;
        Status = status;
    }
}
