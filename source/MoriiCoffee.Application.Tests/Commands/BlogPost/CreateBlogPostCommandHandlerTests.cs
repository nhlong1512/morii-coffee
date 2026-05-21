using AutoMapper;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.BlogPost.CreateBlogPost;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Commands.BlogPost;

public class CreateBlogPostCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IBlogPostsRepository> _postsRepo = new();
    private readonly Mock<IBlogCategoriesRepository> _categoriesRepo = new();
    private readonly Mock<IMapper> _mapper = new();
    private readonly CreateBlogPostCommandHandler _handler;

    public CreateBlogPostCommandHandlerTests()
    {
        _unitOfWork.Setup(x => x.BlogPosts).Returns(_postsRepo.Object);
        _unitOfWork.Setup(x => x.BlogCategories).Returns(_categoriesRepo.Object);
        _handler = new CreateBlogPostCommandHandler(_unitOfWork.Object, _mapper.Object);
    }

    [Fact]
    public async Task Handle_ValidDraft_CreatesPostAndCommits()
    {
        var categoryId = Guid.NewGuid();
        _postsRepo.Setup(x => x.SlugExistsAsync(It.IsAny<string>(), null)).ReturnsAsync(false);
        _categoriesRepo.Setup(x => x.GetByIdAsync(categoryId)).ReturnsAsync(BlogCategoryEntity.Create("Guides", "guides", null, 1, true));
        _postsRepo.Setup(x => x.CreateAsync(It.IsAny<BlogPostEntity>())).Returns(Task.CompletedTask);
        _postsRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, object>>[]>()))
            .ReturnsAsync((Guid id, System.Linq.Expressions.Expression<Func<BlogPostEntity, object>>[] _) => new BlogPostEntity { Id = id, Title = "Post", Slug = "post" });
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(x => x.Map<BlogPostDetailDto>(It.IsAny<BlogPostEntity>())).Returns(new BlogPostDetailDto { Title = "Post" });

        var command = new CreateBlogPostCommand(new CreateBlogPostDto
        {
            Title = "Post",
            ContentHtml = "<p>Hello</p>",
            ContentJson = "{\"type\":\"doc\"}",
            CategoryIds = new List<Guid> { categoryId }
        });

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Title.Should().Be("Post");
        _unitOfWork.Verify(x => x.CommitAsync(), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicateExplicitSlug_ThrowsBadRequest()
    {
        _postsRepo.Setup(x => x.SlugExistsAsync("custom-slug", null)).ReturnsAsync(true);

        var command = new CreateBlogPostCommand(new CreateBlogPostDto
        {
            Title = "Post",
            Slug = "custom slug",
            ContentHtml = "<p>Hello</p>"
        });

        await _handler.Invoking(x => x.Handle(command, CancellationToken.None))
            .Should().ThrowAsync<BadRequestException>()
            .WithMessage("*custom-slug*");
    }

    [Fact]
    public async Task Handle_DuplicateGeneratedSlug_AppendsSuffix()
    {
        BlogPostEntity? createdPost = null;

        _postsRepo.Setup(x => x.SlugExistsAsync("post", null)).ReturnsAsync(true);
        _postsRepo.Setup(x => x.CreateAsync(It.IsAny<BlogPostEntity>()))
            .Callback<BlogPostEntity>(post => createdPost = post)
            .Returns(Task.CompletedTask);
        _postsRepo.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<System.Linq.Expressions.Expression<Func<BlogPostEntity, object>>[]>()))
            .ReturnsAsync((Guid id, System.Linq.Expressions.Expression<Func<BlogPostEntity, object>>[] _) => new BlogPostEntity { Id = id, Title = "Post", Slug = createdPost!.Slug });
        _unitOfWork.Setup(x => x.CommitAsync()).ReturnsAsync(1);
        _mapper.Setup(x => x.Map<BlogPostDetailDto>(It.IsAny<BlogPostEntity>())).Returns(new BlogPostDetailDto { Title = "Post" });

        var command = new CreateBlogPostCommand(new CreateBlogPostDto
        {
            Title = "Post",
            ContentHtml = "<p>Hello</p>"
        });

        await _handler.Handle(command, CancellationToken.None);

        createdPost.Should().NotBeNull();
        createdPost!.Slug.Should().StartWith("post-");
    }
}
