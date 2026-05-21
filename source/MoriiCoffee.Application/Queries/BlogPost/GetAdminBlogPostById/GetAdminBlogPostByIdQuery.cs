using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.BlogPost.GetAdminBlogPostById;

/// <summary>
/// Query for retrieving one admin-visible blog post by ID.
/// </summary>
public record GetAdminBlogPostByIdQuery(Guid BlogPostId) : IQuery<BlogPostDetailDto>;
