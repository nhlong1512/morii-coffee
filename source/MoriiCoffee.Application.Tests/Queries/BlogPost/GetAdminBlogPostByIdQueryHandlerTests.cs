using AutoMapper;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.BlogPost.GetAdminBlogPostById;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Queries.BlogPost;

public class GetAdminBlogPostByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly GetAdminBlogPostByIdQueryHandler _handler;

    public GetAdminBlogPostByIdQueryHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _handler = new GetAdminBlogPostByIdQueryHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_ExistingPost_ReturnsMappedDetail()
    {
        var post = BlogPostEntity.Create("Post", "post", null, "{\"type\":\"doc\"}", "<p>x</p>", null, null, null, null, false, 0, MoriiCoffee.Domain.Shared.Enums.Blog.EBlogPostStatus.Draft);
        var queryable = new List<BlogPostEntity> { post }.BuildMock();
        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), false))
            .Returns(queryable);
        _mapper.Setup(x => x.Map<BlogPostDetailDto>(post)).Returns(new BlogPostDetailDto { Id = post.Id, Title = post.Title });

        var result = await _handler.Handle(new GetAdminBlogPostByIdQuery(post.Id), CancellationToken.None);

        result.Id.Should().Be(post.Id);
        result.Title.Should().Be("Post");
    }

    [Fact]
    public async Task Handle_MissingPost_ThrowsNotFound()
    {
        var queryable = new List<BlogPostEntity>().BuildMock();
        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), false))
            .Returns(queryable);

        await _handler.Invoking(x => x.Handle(new GetAdminBlogPostByIdQuery(Guid.NewGuid()), CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }
}
