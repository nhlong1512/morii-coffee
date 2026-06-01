using FluentAssertions;
using MoriiCoffee.Domain.Aggregates.BlogPostAggregate;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using Xunit;

namespace MoriiCoffee.Domain.Tests.Aggregates;

public class BlogPostAggregateTests
{
    private static BlogPost CreatePost() =>
        BlogPost.Create(
            "Post",
            "post",
            null,
            "{\"type\":\"doc\"}",
            "<p>Post</p>",
            null,
            null,
            null,
            null,
            false,
            0,
            EBlogPostStatus.Draft);

    [Fact]
    public void ReplaceCategories_PreservesExistingAssignments()
    {
        var post = CreatePost();
        var categoryId = Guid.NewGuid();
        post.ReplaceCategories([categoryId]);
        var originalAssignment = post.BlogPostCategories.Single();

        post.ReplaceCategories([categoryId]);

        post.BlogPostCategories.Should().ContainSingle();
        post.BlogPostCategories.Single().Should().BeSameAs(originalAssignment);
    }

    [Fact]
    public void ReplaceCategories_RemovesMissingAndAddsNewAssignments()
    {
        var post = CreatePost();
        var retainedCategoryId = Guid.NewGuid();
        var removedCategoryId = Guid.NewGuid();
        var addedCategoryId = Guid.NewGuid();
        post.ReplaceCategories([retainedCategoryId, removedCategoryId]);
        var retainedAssignment = post.BlogPostCategories
            .Single(link => link.BlogCategoryId == retainedCategoryId);

        post.ReplaceCategories([retainedCategoryId, addedCategoryId]);

        post.BlogPostCategories.Should().HaveCount(2);
        post.BlogPostCategories.Should().Contain(retainedAssignment);
        post.BlogPostCategories.Should().NotContain(link => link.BlogCategoryId == removedCategoryId);
        post.BlogPostCategories.Should().ContainSingle(link => link.BlogCategoryId == addedCategoryId);
    }

    [Fact]
    public void ReplaceCategories_DeduplicatesRequestedAssignments()
    {
        var post = CreatePost();
        var categoryId = Guid.NewGuid();

        post.ReplaceCategories([categoryId, categoryId]);

        post.BlogPostCategories.Should().ContainSingle(link => link.BlogCategoryId == categoryId);
    }

    [Fact]
    public void ReplaceCategories_NewAssignmentsLeaveIdentifierForPersistence()
    {
        var post = CreatePost();

        post.ReplaceCategories([Guid.NewGuid()]);

        post.BlogPostCategories.Single().Id.Should().BeEmpty();
    }
}
