using AutoMapper;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.BlogPost.GetPublicBlogPosts;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using MoriiCoffee.Domain.Shared.SeedWork;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Queries.BlogPost;

public class GetPublicBlogPostsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetPublicBlogPostsQueryHandler _handler;

    public GetPublicBlogPostsQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _handler = new GetPublicBlogPostsQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_ReturnsOnlyPublishedPostsMatchingFilters()
    {
        var category = BlogCategoryEntity.Create("Guides", "guides", null, 1, true);
        var published = BlogPostEntity.Create("Brew Guide", "brew-guide", "Tips", "{\"type\":\"doc\"}", "<p>a</p>", null, null, null, null, false, 2, EBlogPostStatus.Published);
        published.BlogPostCategories.Add(new MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities.BlogPostCategory
        {
            Id = Guid.NewGuid(),
            BlogPostId = published.Id,
            BlogCategoryId = category.Id,
            BlogCategory = category
        });

        var draft = BlogPostEntity.Create("Draft Story", "draft-story", "Hidden", "{\"type\":\"doc\"}", "<p>b</p>", null, null, null, null, false, 1, EBlogPostStatus.Draft);

        _postsRepo.Setup(x => x.FindAll(false)).Returns(new List<BlogPostEntity> { published, draft }.BuildMock());
        _mapper.Setup(x => x.Map<BlogPostSummaryDto>(It.IsAny<BlogPostEntity>()))
            .Returns((BlogPostEntity post) => new BlogPostSummaryDto { Id = post.Id, Title = post.Title, Status = post.Status });

        var result = await _handler.Handle(
            new GetPublicBlogPostsQuery(new PaginationFilter { TakeAll = true }, "guides", "brew", null),
            CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Title.Should().Be("Brew Guide");
        result.Items[0].Status.Should().Be(EBlogPostStatus.Published);
    }
}
