using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Category.CreateCategory;
using MoriiCoffee.Application.Commands.Category.DeleteCategory;
using MoriiCoffee.Application.Commands.Category.UpdateCategory;
using MoriiCoffee.Application.Queries.Category.GetAllCategories;
using MoriiCoffee.Application.Queries.Category.GetCategoryById;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages product categories for the MoriiCoffee catalog.
/// Categories group related products (e.g., Espresso, Cold Brew, Tea, Pastries).
/// </summary>
[ApiController]
[Route("api/v1/categories")]
[Produces("application/json")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<CategoriesController> _logger;

    public CategoriesController(IMediator mediator, ILogger<CategoriesController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Create a new product category.</summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a category",
        Description = "Creates a new product category in the catalog. Category names must be unique.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(CategoryDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> CreateCategory([FromBody] CreateCategoryDto request)
    {
        _logger.LogInformation("POST /api/v1/categories - Creating category: {Name}", request.Name);
        var result = await _mediator.Send(new CreateCategoryCommand(request));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Get a paginated list of all active categories.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all categories",
        Description = "Returns a paginated list of all product categories, ordered by display order.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(CategoryDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetAllCategories([FromQuery] PaginationFilter filter)
    {
        var result = await _mediator.Send(new GetAllCategoriesQuery(filter));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a category by its ID.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get a category by ID",
        Description = "Returns a single category matching the specified ID.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(CategoryDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetCategoryById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetCategoryByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Update an existing category.</summary>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Update a category",
        Description = "Updates the name, description, icon, display order, and active status of a category.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(CategoryDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> UpdateCategory([FromRoute] Guid id, [FromBody] UpdateCategoryDto request)
    {
        var result = await _mediator.Send(new UpdateCategoryCommand(id, request));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Soft-delete a category.</summary>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete a category",
        Description = "Soft-deletes a category. The category is marked as deleted but not removed from the database.")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> DeleteCategory([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteCategoryCommand(id));
        return NoContent();
    }
}
