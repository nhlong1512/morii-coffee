using AutoMapper;
using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPost;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class UpdateBlogPostCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly Mock<IBlogCategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly UpdateBlogPostCommandHandler _handler;

    public UpdateBlogPostCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _unitOfWork.Setup(x => x.BlogCategories).Returns(_categoriesRepo.Object);
        _handler = new UpdateBlogPostCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_MissingPost_ThrowsNotFound()
    {
        var posts = new List<BlogPostEntity>().BuildMock();
        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), true))
            .Returns(posts);

        var command = new UpdateBlogPostCommand(Guid.NewGuid(), new UpdateBlogPostDto { Title = "Post", ContentHtml = "<p>x</p>" });

        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_ValidRequest_UpdatesPostAndCommits()
    {
        var categoryId = Guid.NewGuid();
        var post = BlogPostEntity.Create("Old", "old", null, "{\"type\":\"doc\"}", "<p>old</p>", null, null, null, null, false, 0, MoriiCoffee.Domain.Shared.Enums.Blog.EBlogPostStatus.Draft);
        var posts = new List<BlogPostEntity> { post }.BuildMock();

        _postsRepo.Setup(x => x.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, bool>>>(), true))
            .Returns(posts);
        _postsRepo.Setup(x => x.SlugExistsAsync("new-title", post.Id)).ReturnsAsync(false);
        _postsRepo.Setup(x => x.Update(post)).Returns(Task.CompletedTask);
        _categoriesRepo.Setup(x => x.GetByIdAsync(categoryId))
            .ReturnsAsync(MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory.Create("Guides", "guides", null, 1, true));
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(x => x.Map<BlogPostDetailDto>(post)).Returns(new BlogPostDetailDto { Id = post.Id, Title = "New Title" });

        var command = new UpdateBlogPostCommand(post.Id, new UpdateBlogPostDto
        {
            Title = "New Title",
            ContentHtml = "<p>new</p>",
            ContentJson = "{\"type\":\"doc\"}",
            CategoryIds = new List<Guid> { categoryId }
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("New Title");
        post.Title.Should().Be("New Title");
        post.Slug.Should().Be("new-title");
        post.BlogPostCategories.Should().ContainSingle(x => x.BlogCategoryId == categoryId);
        _unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
    }
}
