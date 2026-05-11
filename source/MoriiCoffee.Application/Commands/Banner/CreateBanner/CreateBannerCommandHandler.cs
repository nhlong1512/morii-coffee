using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Banner.CreateBanner;

/// <summary>
/// Handles creating a new promotional banner.
/// If an image file is provided, it is uploaded to S3 and its CDN URL is stored on the banner.
/// </summary>
public class CreateBannerCommandHandler : ICommandHandler<CreateBannerCommand, BannerDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public CreateBannerCommandHandler(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _mapper = mapper;
    }

    public async Task<BannerDto> Handle(CreateBannerCommand request, CancellationToken cancellationToken)
    {
        var bannerId = Guid.NewGuid();

        string? imageUrl = null;
        if (request.Image != null)
        {
            var s3Key = S3KeyHelper.BuildS3Key(bannerId, request.Image.FileName);
            var uploadResult = await _fileService.UploadAsync(request.Image, FileContainers.BANNERS, s3Key);
            imageUrl = uploadResult.Blob.StorageKey;
        }

        var banner = new Domain.Aggregates.BannerAggregate.Banner
        {
            Id = bannerId,
            Title = request.Title,
            Subtitle = request.Subtitle,
            Cta = request.Cta,
            CtaLink = request.CtaLink,
            DisplayOrder = request.DisplayOrder,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            ImageUrl = imageUrl
        };

        await _unitOfWork.Banners.CreateAsync(banner);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BannerDto>(banner);
    }
}
