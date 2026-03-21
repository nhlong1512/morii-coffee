using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Banner.GetBannerById;

/// <summary>Returns a single non-deleted banner by ID, or 404 if not found.</summary>
public class GetBannerByIdQueryHandler : IQueryHandler<GetBannerByIdQuery, BannerDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetBannerByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BannerDto> Handle(GetBannerByIdQuery request, CancellationToken cancellationToken)
    {
        var banner = await _unitOfWork.Banners.GetByIdAsync(request.BannerId)
            ?? throw new NotFoundException("Banner", request.BannerId);

        return _mapper.Map<BannerDto>(banner);
    }
}
