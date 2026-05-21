using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.BlogCategory.CreateBlogCategory;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;

namespace MoriiCoffee.Application.Tests.Commands.BlogCategory;

public class CreateBlogCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogCategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateBlogCategoryCommandHandler _handler;

    public CreateBlogCategoryCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogCategories).Returns(_categoriesRepo.Object);
        _handler = new CreateBlogCategoryCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_ValidRequest_CreatesCategory()
    {
        _categoriesRepo.Setup(x => x.SlugExistsAsync("guides", null)).ReturnsAsync(false);
        _categoriesRepo.Setup(x => x.CreateAsync(It.IsAny<BlogCategoryEntity>())).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(x => x.Map<BlogCategoryDto>(It.IsAny<BlogCategoryEntity>()))
            .Returns(new BlogCategoryDto { Name = "Guides", Slug = "guides" });

        var result = await _handler.Handle(new CreateBlogCategoryCommand(new CreateBlogCategoryDto
        {
            Name = "Guides",
            Description = "How-to",
            DisplayOrder = 1,
            IsActive = true
        }), CancellationToken.None);

        result.Name.Should().Be("Guides");
        _unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateExplicitSlug_ThrowsBadRequest()
    {
        _categoriesRepo.Setup(x => x.SlugExistsAsync("guides", null)).ReturnsAsync(true);

        await _handler.Invoking(x => x.Handle(new CreateBlogCategoryCommand(new CreateBlogCategoryDto
        {
            Name = "Guides",
            Slug = "guides"
        }), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
