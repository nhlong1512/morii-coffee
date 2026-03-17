using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using BannerEntity = MoriiCoffee.Domain.Aggregates.BannerAggregate.Banner;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Banner.CreateBanner;

/// <summary>
/// Handles banner creation. Optionally uploads an image to MinIO before persisting.
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
        string? imageUrl = null;
        string? imageFileName = null;

        if (request.Image != null)
        {
            var upload = await _fileService.UploadAsync(request.Image, FileContainers.BANNERS);
            imageUrl = upload.Blob.Uri;
            imageFileName = upload.Blob.Name;
        }

        var banner = new BannerEntity
        {
            Id = Guid.NewGuid(),
            Title = request.Title,
            Description = request.Description,
            ImageUrl = imageUrl,
            ImageFileName = imageFileName,
            DisplayOrder = request.DisplayOrder,
            IsActive = request.IsActive
        };

        await _unitOfWork.Banners.CreateAsync(banner);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BannerDto>(banner);
    }
}
