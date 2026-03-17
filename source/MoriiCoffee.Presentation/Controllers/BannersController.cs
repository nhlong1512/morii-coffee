using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Banner.CreateBanner;
using MoriiCoffee.Application.Commands.Banner.DeleteBanner;
using MoriiCoffee.Application.Commands.Banner.ReorderBanners;
using MoriiCoffee.Application.Commands.Banner.ToggleBannerStatus;
using MoriiCoffee.Application.Commands.Banner.UpdateBanner;
using MoriiCoffee.Application.Queries.Banner.GetActiveBanners;
using MoriiCoffee.Application.Queries.Banner.GetAllBanners;
using MoriiCoffee.Application.Queries.Banner.GetBannerById;
using MoriiCoffee.Application.SeedWork.DTOs.Banner;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages promotional banners displayed in the storefront carousel.
/// Public endpoints return active banners only; admin endpoints expose full CRUD.
/// </summary>
[ApiController]
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

    // ─── Public Endpoints ────────────────────────────────────────────────────

    /// <summary>Get all active banners ordered by display position.</summary>
    [HttpGet("api/v1/banners")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get active banners", Description = "Returns all active banners sorted by DisplayOrder for storefront display.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<BannerDto>))]
    public async Task<IActionResult> GetActiveBanners()
    {
        var result = await _mediator.Send(new GetActiveBannersQuery());
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a banner by ID.</summary>
    [HttpGet("api/v1/banners/{id:guid}")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "Get banner by ID")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(BannerDto))]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetBannerById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetBannerByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    // ─── Admin Endpoints ─────────────────────────────────────────────────────

    /// <summary>Get all banners (admin) — active and inactive with pagination.</summary>
    [HttpGet("api/v1/admin/banners")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(Summary = "Admin: get all banners", Description = "Returns all banners including inactive ones, with pagination.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<BannerDto>))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    public async Task<IActionResult> GetAllBanners([FromQuery] PaginationFilter filter)
    {
        var result = await _mediator.Send(new GetAllBannersQuery(filter));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Create a new banner.</summary>
    [HttpPost("api/v1/admin/banners")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(Summary = "Admin: create banner")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(BannerDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    public async Task<IActionResult> CreateBanner([FromForm] CreateBannerDto dto)
    {
        _logger.LogInformation("POST /api/v1/admin/banners — creating banner: {Title}", dto.Title);
        var result = await _mediator.Send(new CreateBannerCommand(dto));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Update an existing banner.</summary>
    [HttpPut("api/v1/admin/banners/{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(Summary = "Admin: update banner")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(BannerDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> UpdateBanner([FromRoute] Guid id, [FromForm] UpdateBannerDto dto)
    {
        var result = await _mediator.Send(new UpdateBannerCommand(id, dto));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Soft-delete a banner.</summary>
    [HttpDelete("api/v1/admin/banners/{id:guid}")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(Summary = "Admin: delete banner")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> DeleteBanner([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteBannerCommand(id));
        return NoContent();
    }

    /// <summary>Toggle a banner's active/inactive status.</summary>
    [HttpPatch("api/v1/admin/banners/{id:guid}/toggle-status")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(Summary = "Admin: toggle banner status")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(BannerDto))]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> ToggleBannerStatus([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new ToggleBannerStatusCommand(id));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Bulk-update the display order of multiple banners.</summary>
    [HttpPut("api/v1/admin/banners/reorder")]
    [Authorize(Roles = "ADMIN")]
    [SwaggerOperation(Summary = "Admin: reorder banners", Description = "Accepts a list of banner ID + DisplayOrder pairs and updates all positions in one request.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(401, SwaggerResponseMessages.Unauthorized)]
    [SwaggerResponse(403, SwaggerResponseMessages.Forbidden)]
    public async Task<IActionResult> ReorderBanners([FromBody] List<ReorderBannerItemDto> items)
    {
        await _mediator.Send(new ReorderBannersCommand(items));
        return Ok(new ApiOkResponse("Banners reordered successfully."));
    }
}
