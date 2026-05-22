using MediatR;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Queries.Store.GetPublicStoreById;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Application.Queries.Store.GetPublicStores;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Public store locator endpoints — no authentication required.
/// Returns only active, non-deleted store locations with opening hours.
/// Supports optional geolocation sorting and city filtering.
/// </summary>
[ApiController]
[Route("api/v1/stores")]
[Produces("application/json")]
public class StoresController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly ILogger<StoresController> _logger;

    public StoresController(IMediator mediator, ILogger<StoresController> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    /// <summary>Get all active stores, optionally sorted by proximity to a given location.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get public store list",
        Description = "Returns all active stores with opening hours. Provide latitude/longitude to sort by distance and optionally filter by radius. City filter and text search are also supported.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<StoreDto>))]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetStores(
        [FromQuery] PaginationFilter filter,
        [FromQuery] double? latitude,
        [FromQuery] double? longitude,
        [FromQuery] double? radius,
        [FromQuery] string? city,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetPublicStoresQuery(filter, latitude, longitude, radius, city, search));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Get a single active store by ID.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get store by ID",
        Description = "Returns a single active, non-deleted store with its full opening hours schedule.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(StoreDto))]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(500, SwaggerResponseMessages.InternalServerError)]
    public async Task<IActionResult> GetStoreById([FromRoute] Guid id)
    {
        _logger.LogInformation("GET /api/v1/stores/{Id}", id);
        var result = await _mediator.Send(new GetPublicStoreByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }
}
