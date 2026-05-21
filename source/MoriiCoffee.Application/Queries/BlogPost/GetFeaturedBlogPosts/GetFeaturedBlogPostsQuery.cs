using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.BlogPost.GetFeaturedBlogPosts;

/// <summary>
/// Query for retrieving featured published blog posts.
/// </summary>
public record GetFeaturedBlogPostsQuery(int Take = 3) : IQuery<List<BlogPostSummaryDto>>;
