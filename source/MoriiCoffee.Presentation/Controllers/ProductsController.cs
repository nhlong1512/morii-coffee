using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Product.CreateProduct;
using MoriiCoffee.Application.Commands.Product.DeleteProduct;
using MoriiCoffee.Application.Commands.Product.DeleteProductImage;
using MoriiCoffee.Application.Commands.Product.ReorderProductImages;
using MoriiCoffee.Application.Commands.Product.UpdateProduct;
using MoriiCoffee.Application.Commands.Product.UploadProductImages;
using MoriiCoffee.Application.Queries.Product.GetPaginatedProducts;
using MoriiCoffee.Application.Queries.Product.GetProductById;
using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages the MoriiCoffee product catalog.
/// Products represent individual menu items (e.g., "Caramel Macchiato", "Cold Brew").
/// Each product can have multiple size variants with different pricing.
/// </summary>
[ApiController]
[Route("api/v1/products")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IMediator mediator, ILogger<ProductsController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    #region Product Management
    /// <summary>Create a new product.</summary>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create a product",
        Description = "Creates a new product in the catalog. If no slug is provided, one will be auto-generated from the product name. Slugs must be unique across all products.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(ProductDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> CreateProduct([FromForm] CreateProductDto request)
    {
        _logger.LogInformation("POST /api/v1/products - Creating product: {Name}", request.Name);
        var result = await _mediator.Send(new CreateProductCommand(request));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Get a paginated list of products with optional filters.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get paginated products",
        Description = "Returns a paginated list of products. Supports filtering by categoryId and isFeatured. Results are ordered by displayOrder then name.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(ProductDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetPaginatedProducts([FromQuery] ProductPaginationFilter filter)
    {
        var result = await _mediator.Send(new GetPaginatedProductsQuery(filter));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a product by ID, including all variants.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get a product by ID",
        Description = "Returns a single product with full details including all size variants and gallery images.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(ProductDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetProductById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetProductByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Update an existing product.</summary>
    [HttpPut("{id:guid}")]
    [SwaggerOperation(
        Summary = "Update a product",
        Description = "Updates all editable fields of a product including name, price, category, status, and featured flag.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(ProductDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> UpdateProduct([FromRoute] Guid id, [FromForm] UpdateProductDto request)
    {
        var result = await _mediator.Send(new UpdateProductCommand(id, request));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Soft-delete a product.</summary>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete a product",
        Description = "Soft-deletes a product. The product is marked as deleted but retained in the database for audit purposes.")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> DeleteProduct([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteProductCommand(id));
        return NoContent();
    }

    #endregion

    #region Image Management
    // ─── Image Management ────────────────────────────────────────────────────────

    /// <summary>Upload one or more gallery images for a product.</summary>
    [HttpPost("{productId:guid}/images")]
    [SwaggerOperation(
        Summary = "Upload product images",
        Description = "Uploads one or more gallery images to the public S3 bucket (moriicoffee-public). Images are stored at products/{productId}/{timestamp}-{filename} and served via CloudFront CDN. The first image uploaded to an empty product is automatically set as the thumbnail. Supported types: jpg, jpeg, png, webp. Max size: 5 MB each. Max images per product: 10.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(List<ProductImageDto>))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> UploadProductImages(
        [FromRoute] Guid productId,
        [FromForm] UploadProductImagesDto request)
    {
        _logger.LogInformation(
            "POST /api/v1/products/{ProductId}/images - Uploading {Count} image(s)",
            productId, request.Files.Count);

        var result = await _mediator.Send(new UploadProductImagesCommand(productId, request.Files));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Delete a product gallery image.</summary>
    [HttpDelete("{productId:guid}/images/{imageId:guid}")]
    [SwaggerOperation(
        Summary = "Delete a product image",
        Description = "Permanently deletes the image record from the database and the file from S3. " +
                      "If the deleted image was the thumbnail, the next image by display order is automatically promoted.")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> DeleteProductImage(
        [FromRoute] Guid productId,
        [FromRoute] Guid imageId)
    {
        _logger.LogInformation(
            "DELETE /api/v1/products/{ProductId}/images/{ImageId}",
            productId, imageId);

        await _mediator.Send(new DeleteProductImageCommand(productId, imageId));
        return NoContent();
    }

    /// <summary>Reorder product gallery images.</summary>
    [HttpPatch("{productId:guid}/images/reorder")]
    [SwaggerOperation(
        Summary = "Reorder product images",
        Description = "Assigns new DisplayOrder values to all gallery images for a product based on the " +
                      "supplied list. Every image for the product must be included. " +
                      "The first ID in the list becomes order 1, the second order 2, etc.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(List<ProductImageDto>))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> ReorderProductImages(
        [FromRoute] Guid productId,
        [FromBody] ReorderProductImagesDto request)
    {
        _logger.LogInformation(
            "PATCH /api/v1/products/{ProductId}/images/reorder",
            productId);

        var result = await _mediator.Send(new ReorderProductImagesCommand(productId, request.ImageIds));
        return Ok(new ApiOkResponse(result));
    }
    #endregion
}
