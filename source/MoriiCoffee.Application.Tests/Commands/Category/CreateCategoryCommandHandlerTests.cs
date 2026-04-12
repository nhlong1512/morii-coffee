using AutoMapper;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using MoriiCoffee.Application.Commands.Category.CreateCategory;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.DTOs.File;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;

namespace MoriiCoffee.Application.Tests.Commands.Category;

public class CreateCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IFileService> _fileService = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateCategoryCommandHandler _handler;

    public CreateCategoryCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepo.Object);
        _handler = new CreateCategoryCommandHandler(_unitOfWork.Object, _fileService.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_SuccessWithoutIcon_CommitsAndReturnsCategoryDto()
    {
        _categoriesRepo.Setup(r => r.GetByNameAsync("Cold Brew")).ReturnsAsync((CategoryEntity?)null);
        _categoriesRepo.Setup(r => r.CreateAsync(It.IsAny<CategoryEntity>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<CategoryDto>(It.IsAny<CategoryEntity>()))
            .Returns(new CategoryDto { Name = "Cold Brew" });

        var cmd = new CreateCategoryCommand(new CreateCategoryDto { Name = "Cold Brew", DisplayOrder = 1 });
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        result.Name.Should().Be("Cold Brew");
        _unitOfWork.Verify(u => u.CommitAsync(), Times.Once);
        _fileService.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SuccessWithIcon_UploadsFileAndCommits()
    {
        _categoriesRepo.Setup(r => r.GetByNameAsync(It.IsAny<string>())).ReturnsAsync((CategoryEntity?)null);
        _categoriesRepo.Setup(r => r.CreateAsync(It.IsAny<CategoryEntity>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(m => m.Map<CategoryDto>(It.IsAny<CategoryEntity>()))
            .Returns(new CategoryDto { Name = "Espresso" });

        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("icon.png");
        var blobResponse = new BlobResponseDto { Blob = new BlobDto { Uri = "https://cdn.test/icon.png", Name = "icon.png" } };
        _fileService.Setup(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()))
            .ReturnsAsync(blobResponse);

        var dto = new CreateCategoryDto { Name = "Espresso", DisplayOrder = 2, Icon = fileMock.Object };
        var cmd = new CreateCategoryCommand(dto);
        var result = await _handler.Handle(cmd, CancellationToken.None);

        result.Should().NotBeNull();
        _fileService.Verify(f => f.UploadAsync(It.IsAny<IFormFile>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NameAlreadyExists_ThrowsBadRequestException()
    {
        var existing = new CategoryEntity { Id = Guid.NewGuid(), Name = "Cold Brew" };
        _categoriesRepo.Setup(r => r.GetByNameAsync("Cold Brew")).ReturnsAsync(existing);

        var cmd = new CreateCategoryCommand(new CreateCategoryDto { Name = "Cold Brew", DisplayOrder = 1 });

        await _handler.Invoking(h => h.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
