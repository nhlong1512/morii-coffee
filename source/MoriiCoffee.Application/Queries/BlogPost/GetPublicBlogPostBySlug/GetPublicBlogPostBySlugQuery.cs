using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.BlogPost.GetPublicBlogPostBySlug;

/// <summary>
/// Query for retrieving one published blog post by public slug.
/// </summary>
public record GetPublicBlogPostBySlugQuery(string Slug) : IQuery<BlogPostDetailDto>;
