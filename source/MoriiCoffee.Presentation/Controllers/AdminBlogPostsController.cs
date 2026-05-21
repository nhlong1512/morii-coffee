using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.BlogPost.CreateBlogPost;
using MoriiCoffee.Application.Commands.BlogPost.DeleteBlogPost;
using MoriiCoffee.Application.Commands.BlogPost.ReorderBlogPosts;
using MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPost;
using MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPostStatus;
using MoriiCoffee.Application.Queries.BlogPost.GetAdminBlogPostById;
using MoriiCoffee.Application.Queries.BlogPost.GetAdminBlogPosts;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Internal admin endpoints for managing blog posts across all lifecycle states.
/// </summary>
[ApiController]
[Route("api/v1/admin/blog-posts")]
[Produces("application/json")]
[Authorize(Roles = $"{nameof(ERole.ADMIN)},{nameof(ERole.STAFF)}")]
public class AdminBlogPostsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminBlogPostsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns a paginated list of blog posts for admin/staff users.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get admin blog posts",
        Description = "Returns blog posts across all lifecycle states for internal management. Supports paging, status filter, category filter, and text search.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<BlogPostSummaryDto>))]
    public async Task<IActionResult> GetAdminBlogPosts(
        [FromQuery] PaginationFilter filter,
        [FromQuery] EBlogPostStatus? status,
        [FromQuery] Guid? categoryId,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetAdminBlogPostsQuery(filter, status, categoryId, search));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Returns a single blog post with full admin detail.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get admin blog post by ID",
        Description = "Returns the editable detail view of a blog post, including content, SEO fields, and linked categories.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(BlogPostDetailDto))]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetAdminBlogPostById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetAdminBlogPostByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Creates a new blog post.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Create a blog post",
        Description = "Creates a new blog post from JSON content, HTML snapshot, metadata, and category links.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(BlogPostDetailDto))]
    public async Task<IActionResult> CreateBlogPost([FromBody] CreateBlogPostDto dto)
    {
        var result = await _mediator.Send(new CreateBlogPostCommand(dto));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Updates an existing blog post.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Update a blog post",
        Description = "Updates editable post fields, including content, SEO metadata, categories, featured flag, display order, and status.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(BlogPostDetailDto))]
    public async Task<IActionResult> UpdateBlogPost([FromRoute] Guid id, [FromBody] UpdateBlogPostDto dto)
    {
        var result = await _mediator.Send(new UpdateBlogPostCommand(id, dto));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Soft-deletes a blog post.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Delete a blog post",
        Description = "Soft-deletes a blog post so it disappears from public and admin default lists while preserving historical data.")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    public async Task<IActionResult> DeleteBlogPost([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteBlogPostCommand(id));
        return NoContent();
    }

    /// <summary>Changes the publication state of a blog post.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Update blog post status",
        Description = "Changes the post lifecycle state between Draft, Published, and Archived. Only publish-ready posts may move to Published.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(BlogPostDetailDto))]
    public async Task<IActionResult> UpdateBlogPostStatus([FromRoute] Guid id, [FromBody] UpdateBlogPostStatusDto dto)
    {
        var result = await _mediator.Send(new UpdateBlogPostStatusCommand(id, dto));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Updates blog post ordering in batch.</summary>
    [HttpPatch("reorder")]
    [SwaggerOperation(
        Summary = "Reorder blog posts",
        Description = "Updates displayOrder for multiple blog posts in a single request. Available to ADMIN and STAFF users.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    public async Task<IActionResult> ReorderBlogPosts([FromBody] ReorderBlogPostsDto dto)
    {
        await _mediator.Send(new ReorderBlogPostsCommand(dto));
        return Ok(new ApiOkResponse());
    }
}
