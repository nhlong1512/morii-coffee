using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Banner.UpdateBanner;

/// <summary>
/// Handles updating the metadata of an existing banner.
/// If an image file is provided, it is uploaded to S3 and the CDN URL replaces the current one.
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

        banner.Title = request.Title;
        banner.Subtitle = request.Subtitle;
        banner.Cta = request.Cta;
        banner.CtaLink = request.CtaLink;
        banner.DisplayOrder = request.DisplayOrder;
        banner.StartDate = request.StartDate;
        banner.EndDate = request.EndDate;
        banner.IsActive = request.IsActive;

        if (request.Image != null)
        {
            var s3Key = S3KeyHelper.BuildS3Key(banner.Id, request.Image.FileName);
            var uploadResult = await _fileService.UploadAsync(request.Image, FileContainers.BANNERS, s3Key);
            banner.ImageUrl = uploadResult.Blob.Uri;
        }

        await _unitOfWork.Banners.Update(banner);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BannerDto>(banner);
    }
}
