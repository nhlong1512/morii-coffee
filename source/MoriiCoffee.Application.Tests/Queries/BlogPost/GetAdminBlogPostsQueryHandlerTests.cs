using AutoMapper;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.BlogPost.GetAdminBlogPosts;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using MoriiCoffee.Domain.Shared.SeedWork;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Queries.BlogPost;

public class GetAdminBlogPostsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetAdminBlogPostsQueryHandler _handler;

    public GetAdminBlogPostsQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _handler = new GetAdminBlogPostsQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_WithSearchAndStatusFilter_ReturnsMatchingItems()
    {
        var category = BlogCategoryEntity.Create("Guides", "guides", null, 1, true);
        var first = BlogPostEntity.Create("Brew Guide", "brew-guide", "Filter tips", "{\"type\":\"doc\"}", "<p>a</p>", null, null, null, null, false, 2, EBlogPostStatus.Published);
        first.BlogPostCategories.Add(new MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities.BlogPostCategory
        {
            Id = Guid.NewGuid(),
            BlogPostId = first.Id,
            BlogCategoryId = category.Id,
            BlogCategory = category
        });
        var second = BlogPostEntity.Create("Archived Story", "archived-story", "Old", "{\"type\":\"doc\"}", "<p>b</p>", null, null, null, null, false, 1, EBlogPostStatus.Archived);

        var queryable = new List<BlogPostEntity> { first, second }.BuildMock();
        _postsRepo.Setup(x => x.FindAll(false)).Returns(queryable);
        _mapper.Setup(x => x.Map<BlogPostSummaryDto>(It.IsAny<BlogPostEntity>()))
            .Returns((BlogPostEntity post) => new BlogPostSummaryDto
            {
                Id = post.Id,
                Title = post.Title,
                Status = post.Status
            });

        var result = await _handler.Handle(
            new GetAdminBlogPostsQuery(new PaginationFilter { TakeAll = true }, EBlogPostStatus.Published, null, "brew"),
            CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Brew Guide");
    }

    [Fact]
    public async Task Handle_WithNoMatches_ReturnsEmptyPagination()
    {
        var queryable = new List<BlogPostEntity>().BuildMock();
        _postsRepo.Setup(x => x.FindAll(false)).Returns(queryable);

        var result = await _handler.Handle(
            new GetAdminBlogPostsQuery(new PaginationFilter { TakeAll = true }, null, null, null),
            CancellationToken.None);

        result.Items.Should().BeEmpty();
        result.Metadata.TotalCount.Should().Be(0);
    }
}
