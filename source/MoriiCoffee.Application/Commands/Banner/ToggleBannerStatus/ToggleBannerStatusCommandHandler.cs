using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Banner.ToggleBannerStatus;

/// <summary>Flips the IsActive flag of a banner and returns the updated DTO.</summary>
public class ToggleBannerStatusCommandHandler : ICommandHandler<ToggleBannerStatusCommand, BannerDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ToggleBannerStatusCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BannerDto> Handle(ToggleBannerStatusCommand request, CancellationToken cancellationToken)
    {
        var banner = await _unitOfWork.Banners.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Banner", request.Id);

        banner.ToggleStatus();

        await _unitOfWork.Banners.Update(banner);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BannerDto>(banner);
    }
}
