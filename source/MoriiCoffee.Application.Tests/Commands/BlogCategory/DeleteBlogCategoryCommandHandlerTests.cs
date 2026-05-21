using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.BlogCategory.DeleteBlogCategory;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;

namespace MoriiCoffee.Application.Tests.Commands.BlogCategory;

public class DeleteBlogCategoryCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogCategoriesRepository> _categoriesRepo = new();
    private readonly DeleteBlogCategoryCommandHandler _handler;

    public DeleteBlogCategoryCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogCategories).Returns(_categoriesRepo.Object);
        _handler = new DeleteBlogCategoryCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_CategoryInUse_ThrowsBadRequest()
    {
        var category = BlogCategoryEntity.Create("Guides", "guides", null, 1, true);
        _categoriesRepo.Setup(x => x.GetByIdAsync(category.Id)).ReturnsAsync(category);
        _categoriesRepo.Setup(x => x.CountPostsUsingCategoryAsync(category.Id)).ReturnsAsync(2);

        await _handler.Invoking(x => x.Handle(new DeleteBlogCategoryCommand(category.Id), CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
