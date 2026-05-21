using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Mappings;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate.Entities;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Tests.Mappings;

public class BlogMapperTests
{
    private readonly IMapper _mapper;
    private static readonly AwsS3Settings S3Settings = new() { CdnBaseUrl = "https://cdn.test" };

    public BlogMapperTests()
    {
        var config = new MapperConfiguration(cfg => cfg.AddProfile(new BlogMapper(S3Settings)), NullLoggerFactory.Instance);
        config.AssertConfigurationIsValid();
        _mapper = config.CreateMapper();
    }

    [Fact]
    public void BlogPostToSummaryDto_MapsAndResolvesCoverImageUrl()
    {
        var category = BlogCategoryEntity.Create("Guides", "guides", "How-to", 1, true);
        var post = BlogPostEntity.Create("Brew Guide", "brew-guide", "Tips", "{\"type\":\"doc\"}", "<p>x</p>", "blogs/abc/cover.png", "cover.png", null, null, true, 1, EBlogPostStatus.Published);
        post.BlogPostCategories.Add(new BlogPostCategory
        {
            Id = Guid.NewGuid(),
            BlogPostId = post.Id,
            BlogCategoryId = category.Id,
            BlogCategory = category
        });

        var dto = _mapper.Map<BlogPostSummaryDto>(post);

        dto.Title.Should().Be("Brew Guide");
        dto.CoverImageUrl.Should().Be("https://cdn.test/blogs/abc/cover.png");
        dto.IsFeatured.Should().BeTrue();
        dto.Categories.Should().ContainSingle();
        dto.Categories[0].Slug.Should().Be("guides");
    }

    [Fact]
    public void BlogPostToDetailDto_MapsEditableFields()
    {
        var post = BlogPostEntity.Create("Story", "story", "Excerpt", "{\"type\":\"doc\"}", "<p>content</p>", null, "cover.png", "SEO", "Desc", false, 3, EBlogPostStatus.Draft);

        var dto = _mapper.Map<BlogPostDetailDto>(post);

        dto.Title.Should().Be("Story");
        dto.ContentHtml.Should().Be("<p>content</p>");
        dto.ContentJson.Should().Be("{\"type\":\"doc\"}");
        dto.SeoTitle.Should().Be("SEO");
        dto.SeoDescription.Should().Be("Desc");
        dto.CoverImageFileName.Should().Be("cover.png");
    }

    [Fact]
    public void BlogPostToSummaryDto_FiltersDeletedOrDeletedCategoryLinks()
    {
        var activeCategory = BlogCategoryEntity.Create("Guides", "guides", null, 1, true);
        var deletedCategory = BlogCategoryEntity.Create("Hidden", "hidden", null, 2, true);
        deletedCategory.IsDeleted = true;

        var post = BlogPostEntity.Create("Story", "story", null, "{\"type\":\"doc\"}", "<p>x</p>", null, null, null, null, false, 0, EBlogPostStatus.Published);
        post.BlogPostCategories.Add(new BlogPostCategory
        {
            Id = Guid.NewGuid(),
            BlogPostId = post.Id,
            BlogCategoryId = activeCategory.Id,
            BlogCategory = activeCategory
        });
        post.BlogPostCategories.Add(new BlogPostCategory
        {
            Id = Guid.NewGuid(),
            BlogPostId = post.Id,
            BlogCategoryId = deletedCategory.Id,
            BlogCategory = deletedCategory
        });

        var dto = _mapper.Map<BlogPostSummaryDto>(post);

        dto.Categories.Should().ContainSingle();
        dto.Categories[0].Name.Should().Be("Guides");
    }
}
