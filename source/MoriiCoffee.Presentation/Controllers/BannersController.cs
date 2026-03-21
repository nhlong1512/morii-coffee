using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Banner.CreateBanner;
using MoriiCoffee.Application.Commands.Banner.DeleteBanner;
using MoriiCoffee.Application.Commands.Banner.UpdateBanner;
using MoriiCoffee.Application.Queries.Banner.GetAllBanners;
using MoriiCoffee.Application.Queries.Banner.GetBannerById;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages promotional banners displayed on the MoriiCoffee storefront.
/// Each banner has a title, optional subtitle, call-to-action, and an optional image uploaded as part of create/update.
/// </summary>
[ApiController]
[Route("api/v1/banners")]
[Produces("application/json")]
public class BannersController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<BannersController> _logger;

    public BannersController(IMediator mediator, ILogger<BannersController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Get all banners ordered by display order.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all banners",
        Description = "Returns all non-deleted banners sorted by displayOrder ascending.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<BannerDto>))]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetAllBanners()
    {
        var result = await _mediator.Send(new GetAllBannersQuery());
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a single banner by ID.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get a banner by ID",
        Description = "Returns a single banner including its CDN image URL.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(BannerDto))]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetBannerById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetBannerByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Create a new banner.</summary>
    [HttpPost]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Create a banner",
        Description = "Creates a new promotional banner. Optionally include an image file to upload it to S3 in the same request.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(BannerDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> CreateBanner([FromForm] CreateBannerDto request)
    {
        _logger.LogInformation("POST /api/v1/banners - Creating banner: {Title}", request.Title);
        var result = await _mediator.Send(new CreateBannerCommand(request));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Update an existing banner.</summary>
    [HttpPut("{id:guid}")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Update a banner",
        Description = "Updates all editable fields of a banner. Optionally include an image file to replace the current banner image.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(BannerDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> UpdateBanner([FromRoute] Guid id, [FromForm] UpdateBannerDto request)
    {
        _logger.LogInformation("PUT /api/v1/banners/{Id}", id);
        var result = await _mediator.Send(new UpdateBannerCommand(id, request));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Soft-delete a banner.</summary>
    [HttpDelete("{id:guid}")]
    [SwaggerOperation(
        Summary = "Delete a banner",
        Description = "Soft-deletes a banner. The record is retained in the database for audit purposes.")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> DeleteBanner([FromRoute] Guid id)
    {
        _logger.LogInformation("DELETE /api/v1/banners/{Id}", id);
        await _mediator.Send(new DeleteBannerCommand(id));
        return NoContent();
    }
}
