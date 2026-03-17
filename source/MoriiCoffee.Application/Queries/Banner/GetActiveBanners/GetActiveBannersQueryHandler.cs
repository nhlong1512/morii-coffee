using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Banner.GetActiveBanners;

/// <summary>Returns all non-deleted active banners sorted by DisplayOrder ascending.</summary>
public class GetActiveBannersQueryHandler : IQueryHandler<GetActiveBannersQuery, List<BannerDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetActiveBannersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<List<BannerDto>> Handle(GetActiveBannersQuery request, CancellationToken cancellationToken)
    {
        var banners = _unitOfWork.Banners
            .FindByCondition(b => b.IsActive)
            .OrderBy(b => b.DisplayOrder)
            .Select(b => _mapper.Map<BannerDto>(b))
            .ToList();

        return Task.FromResult(banners);
    }
}
