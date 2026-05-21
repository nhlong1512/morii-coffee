using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.BlogCategory.GetAdminBlogCategories;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.SeedWork;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;

namespace MoriiCoffee.Application.Tests.Queries.BlogCategory;

public class GetAdminBlogCategoriesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogCategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetAdminBlogCategoriesQueryHandler _handler;

    public GetAdminBlogCategoriesQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogCategories).Returns(_categoriesRepo.Object);
        _handler = new GetAdminBlogCategoriesQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_SearchFilter_ReturnsMatchingCategories()
    {
        var first = BlogCategoryEntity.Create("Guides", "guides", "How-to", 1, true);
        var second = BlogCategoryEntity.Create("Archive", "archive", "Old posts", 2, false);
        _categoriesRepo.Setup(x => x.FindAll(false)).Returns(new List<BlogCategoryEntity> { first, second }.AsQueryable());
        _mapper.Setup(x => x.Map<BlogCategoryDto>(It.IsAny<BlogCategoryEntity>()))
            .Returns((BlogCategoryEntity category) => new BlogCategoryDto { Id = category.Id, Name = category.Name });

        var result = await _handler.Handle(
            new GetAdminBlogCategoriesQuery(new PaginationFilter { TakeAll = true }, "guide"),
            CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Name.Should().Be("Guides");
    }
}
