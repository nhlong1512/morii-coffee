using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Category.GetAllCategories;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.SeedWork;
using Xunit;
using CategoryEntity = MoriiCoffee.Domain.Aggregates.CategoryAggregate.Category;

namespace MoriiCoffee.Application.Tests.Queries.Category;

public class GetAllCategoriesQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetAllCategoriesQueryHandler _handler;

    public GetAllCategoriesQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Categories).Returns(_categoriesRepo.Object);
        _handler = new GetAllCategoriesQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithCategories_ReturnsOrderedPaginatedResult()
    {
        var c1 = new CategoryEntity { Id = Guid.NewGuid(), Name = "Espresso", DisplayOrder = 2 };
        var c2 = new CategoryEntity { Id = Guid.NewGuid(), Name = "Cold Brew", DisplayOrder = 1 };
        var categories = new List<CategoryEntity> { c1, c2 }.AsQueryable();

        _categoriesRepo.Setup(r => r.FindAll(false)).Returns(categories);
        _mapper.Setup(m => m.Map<CategoryDto>(It.IsAny<CategoryEntity>()))
            .Returns((CategoryEntity c) => new CategoryDto { Name = c.Name });

        var query = new GetAllCategoriesQuery(new PaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Should().NotBeNull();
        result.Items.Should().HaveCount(2);
        result.Items.First().Name.Should().Be("Cold Brew");
    }

    [Fact]
    public async Task Handle_EmptyCategories_ReturnsEmptyPagination()
    {
        _categoriesRepo.Setup(r => r.FindAll(false)).Returns(new List<CategoryEntity>().AsQueryable());
        _mapper.Setup(m => m.Map<CategoryDto>(It.IsAny<CategoryEntity>())).Returns(new CategoryDto());

        var query = new GetAllCategoriesQuery(new PaginationFilter { TakeAll = true });
        var result = await _handler.Handle(query, CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Metadata.TotalCount.Should().Be(0);
    }
}
