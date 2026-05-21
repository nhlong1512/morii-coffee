using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.BlogCategory.CreateBlogCategory;
using MoriiCoffee.Application.Commands.BlogCategory.DeleteBlogCategory;
using MoriiCoffee.Application.Commands.BlogCategory.ReorderBlogCategories;
using MoriiCoffee.Application.Commands.BlogCategory.UpdateBlogCategory;
using MoriiCoffee.Application.Queries.BlogCategory.GetAdminBlogCategories;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Internal admin endpoints for managing blog categories.
/// </summary>
[ApiController]
[Route("api/v1/admin/blog-categories")]
[Produces("application/json")]
[Authorize(Roles = $"{nameof(ERole.ADMIN)},{nameof(ERole.STAFF)}")]
public class AdminBlogCategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminBlogCategoriesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns a paginated list of blog categories for admin/staff users.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get admin blog categories",
        Description = "Returns blog categories for internal management, including inactive categories. Supports paging and text search.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<BlogCategoryDto>))]
    public async Task<IActionResult> GetAdminBlogCategories([FromQuery] PaginationFilter filter, [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetAdminBlogCategoriesQuery(filter, search));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Creates a new blog category.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Create a blog category",
        Description = "Creates a new blog category with a unique slug, optional description, active flag, and display order.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(BlogCategoryDto))]
    public async Task<IActionResult> CreateBlogCategory([FromBody] CreateBlogCategoryDto dto)
    {
        var result = await _mediator.Send(new CreateBlogCategoryCommand(dto));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Updates an existing blog category.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Update a blog category",
        Description = "Updates blog category name, slug, description, active flag, and display order.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(BlogCategoryDto))]
    public async Task<IActionResult> UpdateBlogCategory([FromRoute] Guid id, [FromBody] UpdateBlogCategoryDto dto)
    {
        var result = await _mediator.Send(new UpdateBlogCategoryCommand(id, dto));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Deletes a blog category if it is not linked to any blog post.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Delete a blog category",
        Description = "Soft-deletes a blog category. The request is rejected when the category is still linked to one or more non-deleted blog posts.")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    public async Task<IActionResult> DeleteBlogCategory([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteBlogCategoryCommand(id));
        return NoContent();
    }

    /// <summary>Reorders blog categories in batch.</summary>
    [HttpPatch("reorder")]
    [SwaggerOperation(
        Summary = "Reorder blog categories",
        Description = "Updates displayOrder for multiple blog categories in a single request. Available to ADMIN and STAFF users.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    public async Task<IActionResult> ReorderBlogCategories([FromBody] ReorderBlogCategoriesDto dto)
    {
        await _mediator.Send(new ReorderBlogCategoriesCommand(dto));
        return Ok(new ApiOkResponse());
    }
}
