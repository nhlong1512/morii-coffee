using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Banner.GetAllBanners;

/// <summary>Returns a paginated list of all banners ordered by DisplayOrder.</summary>
public class GetAllBannersQueryHandler : IQueryHandler<GetAllBannersQuery, Pagination<BannerDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllBannersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Pagination<BannerDto>> Handle(GetAllBannersQuery request, CancellationToken cancellationToken)
    {
        var dtoQuery = _unitOfWork.Banners
            .FindAll()
            .OrderBy(b => b.DisplayOrder)
            .AsEnumerable()
            .Select(b => _mapper.Map<BannerDto>(b))
            .AsQueryable();

        var result = PagingHelper.QueryPaginate(request.Filter, dtoQuery);
        return Task.FromResult(result);
    }
}
