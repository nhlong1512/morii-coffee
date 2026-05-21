using AutoMapper;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.BlogPost.GetPublicBlogPostBySlug;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using Xunit;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Queries.BlogPost;

public class GetPublicBlogPostBySlugQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetPublicBlogPostBySlugQueryHandler _handler;

    public GetPublicBlogPostBySlugQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _handler = new GetPublicBlogPostBySlugQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_PublishedPostFound_ReturnsMappedDetail()
    {
        var post = BlogPostEntity.Create("Post", "post", null, "{\"type\":\"doc\"}", "<p>x</p>", null, null, null, null, false, 0, EBlogPostStatus.Published);
        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), false))
            .Returns(new List<BlogPostEntity> { post }.BuildMock());
        _mapper.Setup(x => x.Map<BlogPostDetailDto>(post)).Returns(new BlogPostDetailDto { Id = post.Id, Title = post.Title });

        var result = await _handler.Handle(new GetPublicBlogPostBySlugQuery("post"), CancellationToken.None);

        result.Id.Should().Be(post.Id);
    }

    [Fact]
    public async Task Handle_PublishedPostMissing_ThrowsNotFound()
    {
        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), false))
            .Returns(new List<BlogPostEntity>().BuildMock());

        await _handler.Invoking(x => x.Handle(new GetPublicBlogPostBySlugQuery("missing"), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
