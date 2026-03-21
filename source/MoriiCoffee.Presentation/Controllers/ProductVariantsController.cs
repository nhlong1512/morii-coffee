using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.ProductVariant.DeleteProductVariant;
using MoriiCoffee.Application.Commands.ProductVariant.UpdateProductVariant;
using MoriiCoffee.Application.Queries.ProductVariant.GetVariantById;
using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages individual product variants (size options) by their own ID.
/// Variant-scoped endpoints (get by product, create) live in <see cref="ProductsController"/>
/// under <c>api/v1/products/{productId}/variants</c>.
/// </summary>
[ApiController]
[Route("api/v1/variants")]
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

    /// <summary>Get a single variant by its ID.</summary>
    [HttpGet("{id:guid}")]
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

    /// <summary>Update an existing product variant.</summary>
    [HttpPut("{id:guid}")]
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
    [HttpDelete("{id:guid}")]
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
