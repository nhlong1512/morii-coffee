using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Store.CreateStore;
using MoriiCoffee.Application.Commands.Store.DeleteStore;
using MoriiCoffee.Application.Commands.Store.ReorderStores;
using MoriiCoffee.Application.Commands.Store.UpdateStore;
using MoriiCoffee.Application.Commands.Store.UpdateStoreStatus;
using MoriiCoffee.Application.Queries.Store.GetAdminStoreById;
using MoriiCoffee.Application.Queries.Store.GetAdminStores;
using MoriiCoffee.Application.SeedWork.DTOs.Store;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.SeedWork;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Admin endpoints for managing MoriiCoffee store locations.
/// Includes full CRUD, status toggling, and display order management.
/// ADMIN and STAFF roles may read; only ADMIN may create, update, delete, or change status.
/// </summary>
[ApiController]
[Route("api/v1/admin/stores")]
[Produces("application/json")]
[Authorize(Roles = $"{nameof(ERole.ADMIN)},{nameof(ERole.STAFF)}")]
public class AdminStoresController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminStoresController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Returns a paginated list of stores for admin users, including inactive ones.</summary>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get admin store list",
        Description = "Returns all non-deleted stores (active and inactive) with opening hours. Supports paging, active status filter, city filter, and text search.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(Pagination<StoreDto>))]
    public async Task<IActionResult> GetAdminStores(
        [FromQuery] PaginationFilter filter,
        [FromQuery] bool? isActive,
        [FromQuery] string? city,
        [FromQuery] string? search)
    {
        var result = await _mediator.Send(new GetAdminStoresQuery(filter, isActive, city, search));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Returns a single store by ID for admin editing.</summary>
    [HttpGet("{id:guid}")]
    [SwaggerOperation(
        Summary = "Get admin store by ID",
        Description = "Returns the full editable detail of a store including all 7 opening hours records.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(StoreDto))]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> GetAdminStoreById([FromRoute] Guid id)
    {
        var result = await _mediator.Send(new GetAdminStoreByIdQuery(id));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Creates a new store location.</summary>
    [HttpPost]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Create a store",
        Description = "Creates a new store with all required fields and exactly 7 opening hours entries (one per day). Slug is auto-generated from Name if omitted.")]
    [SwaggerResponse(201, SwaggerResponseMessages.CreatedSuccessfully, typeof(StoreDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(409, SwaggerResponseMessages.Conflict)]
    public async Task<IActionResult> CreateStore([FromBody] CreateStoreDto dto)
    {
        var result = await _mediator.Send(new CreateStoreCommand(dto));
        return StatusCode(201, new ApiCreatedResponse(result));
    }

    /// <summary>Fully updates an existing store, replacing all opening hours.</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Update a store",
        Description = "Performs a full update of an existing store. All 7 opening hours records are deleted and replaced with the provided values.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(StoreDto))]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    [SwaggerResponse(409, SwaggerResponseMessages.Conflict)]
    public async Task<IActionResult> UpdateStore([FromRoute] Guid id, [FromBody] CreateStoreDto dto)
    {
        var result = await _mediator.Send(new UpdateStoreCommand(id, dto));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Soft-deletes a store, hiding it from all listings.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Delete a store",
        Description = "Soft-deletes a store (IsDeleted = true). The record is preserved in the database for historical reference.")]
    [SwaggerResponse(204, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> DeleteStore([FromRoute] Guid id)
    {
        await _mediator.Send(new DeleteStoreCommand(id));
        return NoContent();
    }

    /// <summary>Toggles the active/inactive status of a store without a full update.</summary>
    [HttpPatch("{id:guid}/status")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(
        Summary = "Update store status",
        Description = "Sets the IsActive flag of a store. Inactive stores are hidden from the public store locator but remain visible in admin views.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(StoreDto))]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> UpdateStoreStatus([FromRoute] Guid id, [FromBody] UpdateStoreStatusDto dto)
    {
        var result = await _mediator.Send(new UpdateStoreStatusCommand(id, dto));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Bulk-updates the display order of multiple stores in a single request.</summary>
    [HttpPatch("reorder")]
    [SwaggerOperation(
        Summary = "Reorder stores",
        Description = "Updates displayOrder for multiple stores at once. Available to ADMIN and STAFF users. The public store locator will reflect the new order immediately.")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> ReorderStores([FromBody] ReorderStoresDto dto)
    {
        await _mediator.Send(new ReorderStoresCommand(dto));
        return Ok(new ApiOkResponse());
    }
}
