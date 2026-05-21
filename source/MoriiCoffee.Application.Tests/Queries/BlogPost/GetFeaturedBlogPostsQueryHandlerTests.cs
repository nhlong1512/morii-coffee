using AutoMapper;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.BlogPost.GetFeaturedBlogPosts;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using Xunit;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Queries.BlogPost;

public class GetFeaturedBlogPostsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetFeaturedBlogPostsQueryHandler _handler;

    public GetFeaturedBlogPostsQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _handler = new GetFeaturedBlogPostsQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_ReturnsFeaturedPublishedPostsOnly()
    {
        var featured = BlogPostEntity.Create("Featured", "featured", null, "{\"type\":\"doc\"}", "<p>x</p>", null, null, null, null, true, 1, EBlogPostStatus.Published);
        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), false))
            .Returns(new List<BlogPostEntity> { featured }.BuildMock());
        _mapper.Setup(x => x.Map<BlogPostSummaryDto>(It.IsAny<BlogPostEntity>()))
            .Returns((BlogPostEntity post) => new BlogPostSummaryDto { Id = post.Id, Title = post.Title, IsFeatured = post.IsFeatured });

        var result = await _handler.Handle(new GetFeaturedBlogPostsQuery(3), CancellationToken.None);

        result.Should().ContainSingle();
        result[0].Title.Should().Be("Featured");
    }
}
