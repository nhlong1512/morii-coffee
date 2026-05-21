using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.BlogCategory.GetPublicBlogCategories;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;

namespace MoriiCoffee.Application.Tests.Queries.BlogCategory;

public class GetPublicBlogCategoriesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogCategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetPublicBlogCategoriesQueryHandler _handler;

    public GetPublicBlogCategoriesQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogCategories).Returns(_categoriesRepo.Object);
        _handler = new GetPublicBlogCategoriesQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_ActiveOnly_FiltersInactiveCategories()
    {
        var active = BlogCategoryEntity.Create("Guides", "guides", null, 1, true);
        var inactive = BlogCategoryEntity.Create("Archive", "archive", null, 2, false);

        _categoriesRepo.Setup(x => x.FindAll(false)).Returns(new List<BlogCategoryEntity> { active, inactive }.AsQueryable());
        _mapper.Setup(x => x.Map<BlogCategoryDto>(It.IsAny<BlogCategoryEntity>()))
            .Returns((BlogCategoryEntity category) => new BlogCategoryDto { Id = category.Id, Name = category.Name, IsActive = category.IsActive });

        var result = await _handler.Handle(new GetPublicBlogCategoriesQuery(true), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Name.Should().Be("Guides");
    }
}
