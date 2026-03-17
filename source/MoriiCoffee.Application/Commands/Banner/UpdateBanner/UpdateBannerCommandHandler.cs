using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Banner.UpdateBanner;

/// <summary>
/// Updates banner fields. If a new image is provided, the old one is deleted from MinIO first.
/// </summary>
public class UpdateBannerCommandHandler : ICommandHandler<UpdateBannerCommand, BannerDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public UpdateBannerCommandHandler(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _mapper = mapper;
    }

    public async Task<BannerDto> Handle(UpdateBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = await _unitOfWork.Banners.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Banner", request.Id);

        if (request.Image != null)
        {
            if (!string.IsNullOrEmpty(banner.ImageFileName))
                await _fileService.DeleteAsync(FileContainers.BANNERS, banner.ImageFileName);

            var upload = await _fileService.UploadAsync(request.Image, FileContainers.BANNERS);
            banner.SetImage(upload.Blob.Uri, upload.Blob.Name);
        }

        banner.Title = request.Title;
        banner.Description = request.Description;
        banner.DisplayOrder = request.DisplayOrder;
        banner.IsActive = request.IsActive;

        await _unitOfWork.Banners.Update(banner);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BannerDto>(banner);
    }
}
