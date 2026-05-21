using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.BlogPost.DeleteBlogPost;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class DeleteBlogPostCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly DeleteBlogPostCommandHandler _handler;

    public DeleteBlogPostCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _handler = new DeleteBlogPostCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ExistingPost_SoftDeletesAndCommits()
    {
        var post = new BlogPostEntity { Id = Guid.NewGuid(), Title = "Post", Slug = "post" };
        _postsRepo.Setup(x => x.GetByIdAsync(post.Id)).ReturnsAsync(post);
        _postsRepo.Setup(x => x.SoftDelete(post)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new DeleteBlogPostCommand(post.Id), CancellationToken.None);

        result.Should().BeTrue();
        _unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
    }
}
