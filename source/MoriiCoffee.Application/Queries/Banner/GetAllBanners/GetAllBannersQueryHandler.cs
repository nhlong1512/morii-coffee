using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Banner.GetAllBanners;

/// <summary>Returns all non-deleted banners ordered by <c>DisplayOrder</c> ascending.</summary>
public class GetAllBannersQueryHandler : IQueryHandler<GetAllBannersQuery, List<BannerDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAllBannersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<BannerDto>> Handle(GetAllBannersQuery request, CancellationToken cancellationToken)
    {
        var banners = await _unitOfWork.Banners.GetAllOrderedAsync();
        return _mapper.Map<List<BannerDto>>(banners);
    }
}
