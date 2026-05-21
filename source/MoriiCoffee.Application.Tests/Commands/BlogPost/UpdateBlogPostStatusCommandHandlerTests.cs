using AutoMapper;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPostStatus;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using Xunit;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class UpdateBlogPostStatusCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<Domain.Repositories.IBlogPostsRepository> _postsRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateBlogPostStatusCommandHandler _handler;

    public UpdateBlogPostStatusCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _handler = new UpdateBlogPostStatusCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_PublishablePost_UpdatesStatus()
    {
        var post = BlogPostEntity.Create("Post", "post", null, "{\"type\":\"doc\"}", "<p>x</p>", null, null, null, null, false, 0, EBlogPostStatus.Draft);
        post.ReplaceCategories(new[] { Guid.NewGuid() });

        var posts = new List<BlogPostEntity> { post }.BuildMock();
        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), true))
            .Returns(posts);
        _postsRepo.Setup(x => x.Update(post)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(x => x.Map<BlogPostDetailDto>(post)).Returns(new BlogPostDetailDto { Status = EBlogPostStatus.Published });

        var result = await _handler.Handle(new UpdateBlogPostStatusCommand(post.Id, new UpdateBlogPostStatusDto { Status = EBlogPostStatus.Published }), CancellationToken.None);

        result.Status.Should().Be(EBlogPostStatus.Published);
    }

    [Fact]
    public async Task Handle_PublishWithoutCategories_ThrowsBadRequest()
    {
        var post = BlogPostEntity.Create("Post", "post", null, "{\"type\":\"doc\"}", "<p>x</p>", null, null, null, null, false, 0, EBlogPostStatus.Draft);

        var posts = new List<BlogPostEntity> { post }.BuildMock();
        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), true))
            .Returns(posts);

        await _handler.Invoking(x => x.Handle(
                new UpdateBlogPostStatusCommand(post.Id, new UpdateBlogPostStatusDto { Status = EBlogPostStatus.Published }),
                CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>();
    }
}
