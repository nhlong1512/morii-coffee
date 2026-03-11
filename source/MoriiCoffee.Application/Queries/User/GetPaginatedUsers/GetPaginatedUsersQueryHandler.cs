using AutoMapper;
using Microsoft.AspNetCore.Identity;
using MoriiCoffee.Application.SeedWork.DTOs.User;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;
using MoriiCoffee.Domain.SeedWork.Query;
using UserEntity = MoriiCoffee.Domain.Aggregates.UserAggregate.User;

namespace MoriiCoffee.Application.Queries.User.GetPaginatedUsers;

/// <summary>Returns a paginated, optionally filtered list of users.</summary>
public class GetPaginatedUsersQueryHandler
    : IQueryHandler<GetPaginatedUsersQuery, Pagination<UserSummaryDto>>
{
    private readonly UserManager<UserEntity> _userManager;
    private readonly IMapper _mapper;

    public GetPaginatedUsersQueryHandler(UserManager<UserEntity> userManager, IMapper mapper)
    {
        _userManager = userManager;
        _mapper = mapper;
    }

    public Task<Pagination<UserSummaryDto>> Handle(
        GetPaginatedUsersQuery request, CancellationToken cancellationToken)
    {
        IQueryable<UserEntity> query = _userManager.Users;

        if (request.Status.HasValue)
            query = query.Where(u => u.Status == request.Status.Value);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var lower = request.Search.ToLowerInvariant();
            query = query.Where(u =>
                (u.Email != null && u.Email.Contains(lower)) ||
                (u.UserName != null && u.UserName.Contains(lower)) ||
                (u.FullName != null && u.FullName.Contains(lower)));
        }

        var userPage = PagingHelper.QueryPaginate(request.Filter, query.OrderBy(u => u.CreatedAt));

        var result = new Pagination<UserSummaryDto>
        {
            Items = userPage.Items.Select(u => _mapper.Map<UserSummaryDto>(u)).ToList(),
            Metadata = userPage.Metadata
        };

        return Task.FromResult(result);
    }
}
