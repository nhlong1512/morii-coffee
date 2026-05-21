using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.BlogPost.ReorderBlogPosts;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class ReorderBlogPostsCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly ReorderBlogPostsCommandHandler _handler;

    public ReorderBlogPostsCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _handler = new ReorderBlogPostsCommandHandler(_unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_ValidItems_UpdatesOrdersAndCommits()
    {
        var post = new BlogPostEntity { Id = Guid.NewGuid(), Title = "Post", Slug = "post" };
        _postsRepo.Setup(x => x.GetByIdAsync(post.Id)).ReturnsAsync(post);
        _postsRepo.Setup(x => x.Update(post)).Returns(Task.CompletedTask);
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(new ReorderBlogPostsCommand(new ReorderBlogPostsDto
        {
            Items = new List<ReorderBlogPostsItemDto> { new() { Id = post.Id, DisplayOrder = 3 } }
        }), CancellationToken.None);

        result.Should().BeTrue();
        post.DisplayOrder.Should().Be(3);
    }
}
