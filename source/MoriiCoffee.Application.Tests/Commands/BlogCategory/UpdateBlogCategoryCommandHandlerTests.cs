using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.BlogCategory.UpdateBlogCategory;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;

namespace MoriiCoffee.Application.Tests.Commands.BlogCategory;

public class UpdateBlogCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogCategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateBlogCategoryCommandHandler _handler;

    public UpdateBlogCategoryCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogCategories).Returns(_categoriesRepo.Object);
        _handler = new UpdateBlogCategoryCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_MissingCategory_ThrowsNotFound()
    {
        _categoriesRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((BlogCategoryEntity?)null);

        await _handler.Invoking(x => x.Handle(new UpdateBlogCategoryCommand(Guid.NewGuid(), new UpdateBlogCategoryDto
        {
            Name = "Guides"
        }), CancellationToken.None)).Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesCategory()
    {
        var category = BlogCategoryEntity.Create("Old", "old", null, 1, true);
        _categoriesRepo.Setup(x => x.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _categoriesRepo.Setup(x => x.SlugExistsAsync("new-guides", category.Id)).ReturnsAsync(false);
        _categoriesRepo.Setup(x => x.Update(category)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(x => x.Map<BlogCategoryDto>(category)).Returns(new BlogCategoryDto { Id = category.Id, Name = "New Guides" });

        var result = await _handler.Handle(new UpdateBlogCategoryCommand(category.Id, new UpdateBlogCategoryDto
        {
            Name = "New Guides",
            DisplayOrder = 2,
            IsActive = false
        }), CancellationToken.None);

        result.Name.Should().Be("New Guides");
        category.Slug.Should().Be("new-guides");
    }
}
