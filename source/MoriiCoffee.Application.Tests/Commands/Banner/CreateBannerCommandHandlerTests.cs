using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using MoriiCoffee.Application.Commands.Banner.CreateBanner;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BannerEntity = MoriiCoffee.Domain.Aggregates.BannerAggregate.Banner;

namespace MoriiCoffee.Application.Tests.Commands.Banner;

public class CreateBannerCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBannersRepository> _bannersRepo = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateBannerCommandHandler _handler;

    public CreateBannerCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Banners).Returns(_bannersRepo.Object);
        _handler = new CreateBannerCommandHandler(_unitOfWork.Object, _fileService.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_SuccessWithoutImage_CommitsAndReturnsBannerDto()
    {
        _bannersRepo.Setup(r => r.CreateAsync(It.IsAny<BannerEntity>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<BannerDto>(It.IsAny<BannerEntity>()))
            .Returns(new BannerDto { Title = "Summer Sale" });

        var cmd = new CreateBannerCommand(new CreateBannerDto
        {
            Title = "Summer Sale",
            DisplayOrder = 1,
            IsActive = true
        });

        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Title.Should().Be("Summer Sale");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _fileService.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SuccessWithImage_UploadsFileAndCommits()
    {
        _bannersRepo.Setup(r => r.CreateAsync(It.IsAny<BannerEntity>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<BannerDto>(It.IsAny<BannerEntity>()))
            .Returns(new BannerDto { Title = "Summer Sale" });

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("banner.jpg");
        var blobResponse = new BlobResponseDto { Blob = new BlobDto { Uri = "https://cdn.test/banner.jpg", Name = "banner.jpg" } };
        _fileService.Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(blobResponse);

        var dto = new CreateBannerDto
        {
            Title = "Summer Sale",
            DisplayOrder = 1,
            IsActive = true,
            Image = fileMock.Object
        };
        var cmd = new CreateBannerCommand(dto);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _fileService.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }
}
