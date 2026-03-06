using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.ProductVariant.CreateProductVariant;
using MoriiCoffee.Application.Commands.ProductVariant.DeleteProductVariant;
using MoriiCoffee.Application.Commands.ProductVariant.UpdateProductVariant;
using MoriiCoffee.Application.Queries.ProductVariant.GetVariantById;
using MoriiCoffee.Application.Queries.ProductVariant.GetVariantsByProductId;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages product variants (size options) within the MoriiCoffee catalog.
/// Each variant defines a size (S/M/L/XL), an additional price, and stock information.
/// The total price for a variant = Product.BasePrice + Variant.AdditionalPrice.
/// </summary>
[ApiController]
[Route("api/v1")]
[Produces("application/json")]
public class ProductVariantsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductVariantsController> _logger;

    public ProductVariantsController(IMediator mediator, ILogger<ProductVariantsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Get all variants for a specific product.</summary>
    [HttpGet("products/{productId:guid}/variants")]
    [SwaggerOperation(
        Summary = "Get variants by product",
        Description = "Returns all available size variants for the specified product, ordered by size.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(ProductVariantDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetVariantsByProductId([FromRoute] Guid productId)
    {
        var result = await _mediator.Send(new GetVariantsByProductIdQuery(productId));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a single variant by its ID.</summary>
    [HttpGet("variants/{id:guid}")]
    [SwaggerOperation(
        Summary = "Get a variant by ID",
        Description = "Returns a single product variant including computed total price.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(ProductVariantDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetVariantById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetVariantByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Add a new variant to a product.</summary>
    [HttpPost("products/{productId:guid}/variants")]
    [SwaggerOperation(
        Summary = "Create a product variant",
        Description = """
            Adds a new size variant to an existing product.
            If isDefault is true, all other variants' default flag will be cleared automatically.
            """)]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(ProductVariantDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> CreateVariant(
        [FromRoute] Guid productId,
        [FromBody] CreateProductVariantDto request)
    {
        _logger.LogInformation("POST variants for product {ProductId}", productId);
        var result = await _mediator.Send(new CreateProductVariantCommand(productId, request));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Update an existing product variant.</summary>
    [HttpPut("variants/{id:guid}")]
    [SwaggerOperation(
        Summary = "Update a product variant",
        Description = "Updates the name, size, price, stock, and availability of a product variant.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(ProductVariantDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> UpdateVariant(
        [FromRoute] Guid id,
        [FromBody] UpdateProductVariantDto request)
    {
        var result = await _mediator.Send(new UpdateProductVariantCommand(id, request));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Soft-delete a product variant.</summary>
    [HttpDelete("variants/{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete a product variant",
        Description = "Soft-deletes a product variant. Retained in the database for order history integrity.")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> DeleteVariant([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteProductVariantCommand(id));
        return NoContent();
    }
}
