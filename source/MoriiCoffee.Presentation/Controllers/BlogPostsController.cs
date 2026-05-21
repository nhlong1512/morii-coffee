using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Queries.BlogCategory.GetPublicBlogCategories;
using MoriiCoffee.Application.Queries.BlogPost.GetFeaturedBlogPosts;
using MoriiCoffee.Application.Queries.BlogPost.GetPublicBlogPostBySlug;
using MoriiCoffee.Application.Queries.BlogPost.GetPublicBlogPosts;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Public storefront endpoints for browsing published blog content.
/// </summary>
[ApiController]
[Produces("application/json")]
public class BlogPostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public BlogPostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns published blog posts with optional category and search filters.</summary>
    [HttpGet("api/v1/blog-posts")]
    [SwaggerOperation(
        Summary = "Get published blog posts",
        Description = "Returns storefront-visible blog posts only. Supports pagination, category slug filtering, text search, and simple sort options.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<BlogPostSummaryDto>))]
    public async Task<IActionResult> GetPublicBlogPosts(
        [FromQuery] PaginationFilter filter,
        [FromQuery] string? categorySlug,
        [FromQuery] string? search,
        [FromQuery] string? sort)
    {
        var result = await _mediator.Send(new GetPublicBlogPostsQuery(filter, categorySlug, search, sort));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Returns a published blog post by its public slug.</summary>
    [HttpGet("api/v1/blog-posts/{slug}")]
    [SwaggerOperation(
        Summary = "Get published blog post by slug",
        Description = "Returns a single published blog post by its unique public slug. Draft and archived posts are never exposed here.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(BlogPostDetailDto))]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetPublicBlogPostBySlug([FromRoute] string slug)
    {
        var result = await _mediator.Send(new GetPublicBlogPostBySlugQuery(slug));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Returns featured published blog posts for spotlight sections.</summary>
    [HttpGet("api/v1/blog-posts/featured")]
    [SwaggerOperation(
        Summary = "Get featured blog posts",
        Description = "Returns featured blog posts that are currently published, ordered by display order and publish recency.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<BlogPostSummaryDto>))]
    public async Task<IActionResult> GetFeaturedBlogPosts([FromQuery] int take = 3)
    {
        var result = await _mediator.Send(new GetFeaturedBlogPostsQuery(take));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Returns blog categories available to public consumers.</summary>
    [HttpGet("api/v1/blog-categories")]
    [SwaggerOperation(
        Summary = "Get public blog categories",
        Description = "Returns blog categories for storefront navigation. By default only active categories are returned.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<BlogCategoryDto>))]
    public async Task<IActionResult> GetPublicBlogCategories([FromQuery] bool activeOnly = true)
    {
        var result = await _mediator.Send(new GetPublicBlogCategoriesQuery(activeOnly));
        return Ok(new ApiOkResponse(result));
    }
}
