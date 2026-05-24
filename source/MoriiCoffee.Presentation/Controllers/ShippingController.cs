using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Shipping.CreateShippingQuote;
using MoriiCoffee.Application.Commands.Shipping.CancelShipment;
using MoriiCoffee.Application.Commands.Shipping.CreateShipment;
using MoriiCoffee.Application.Commands.Shipping.RequoteShipment;
using MoriiCoffee.Application.Commands.Shipping.SyncShipment;
using MoriiCoffee.Application.Commands.Shipping.UpdateShipmentNote;
using MoriiCoffee.Application.Queries.Shipping.GetShippingDistricts;
using MoriiCoffee.Application.Queries.Shipping.GetShippingProvinces;
using MoriiCoffee.Application.Queries.Shipping.GetShipmentByOrderId;
using MoriiCoffee.Application.Queries.Shipping.GetShippingWards;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using MoriiCoffee.Domain.Shared.Enums.User;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

[ApiController]
[Route("api/v1/shipping")]
[Produces("application/json")]
public class ShippingController : ControllerBase
{
    private readonly IMediator _mediator;

    public ShippingController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("ghn/provinces")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "[GHN] Get provinces")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<ShippingProvinceDto>))]
    public async Task<IActionResult> GetProvinces()
    {
        var result = await _mediator.Send(new GetShippingProvincesQuery());
        return Ok(new ApiOkResponse(result));
    }

    [HttpGet("ghn/districts")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "[GHN] Get districts by province")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<ShippingDistrictDto>))]
    public async Task<IActionResult> GetDistricts([FromQuery] int provinceId)
    {
        var result = await _mediator.Send(new GetShippingDistrictsQuery(provinceId));
        return Ok(new ApiOkResponse(result));
    }

    [HttpGet("ghn/wards")]
    [AllowAnonymous]
    [SwaggerOperation(Summary = "[GHN] Get wards by district")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(List<ShippingWardDto>))]
    public async Task<IActionResult> GetWards([FromQuery] int districtId)
    {
        var result = await _mediator.Send(new GetShippingWardsQuery(districtId));
        return Ok(new ApiOkResponse(result));
    }

    [HttpPost("quotes")]
    [Authorize]
    [SwaggerOperation(Summary = "[GHN] Create shipping quote from current cart")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(ShippingQuoteDto))]
    public async Task<IActionResult> CreateQuote([FromBody] CreateShippingQuoteDto dto)
    {
        var result = await _mediator.Send(new CreateShippingQuoteCommand
        {
            UserId = GetCurrentUserId(),
            DeliveryMethod = dto.DeliveryMethod,
            PaymentMethod = dto.PaymentMethod,
            Address = dto.Address,
            SelectedServiceId = dto.SelectedServiceId
        });

        return Ok(new ApiOkResponse(result));
    }

    [HttpGet("orders/{orderId:guid}")]
    [Authorize]
    [SwaggerOperation(Summary = "[GHN] Get shipment summary for an order")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(ShipmentSummaryDto))]
    public async Task<IActionResult> GetShipmentByOrderId([FromRoute] Guid orderId)
    {
        var result = await _mediator.Send(new GetShipmentByOrderIdQuery(orderId, GetCurrentUserId(), IsAdmin()));
        return Ok(new ApiOkResponse(result));
    }

    [HttpPost("orders/{orderId:guid}/create")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "[GHN][Admin] Create or retry a shipment for an order")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(ShipmentSummaryDto))]
    public async Task<IActionResult> CreateShipment([FromRoute] Guid orderId)
    {
        var result = await _mediator.Send(new CreateShipmentCommand { OrderId = orderId });
        return Ok(new ApiOkResponse(result));
    }

    [HttpPost("orders/{orderId:guid}/requote")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "[GHN][Admin] Requote a shipment from the stored order snapshot")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(ShippingQuoteDto))]
    public async Task<IActionResult> RequoteShipment([FromRoute] Guid orderId)
    {
        var result = await _mediator.Send(new RequoteShipmentCommand { OrderId = orderId });
        return Ok(new ApiOkResponse(result));
    }

    [HttpPost("orders/{orderId:guid}/sync")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "[GHN][Admin] Sync shipment state from GHN")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(ShipmentSummaryDto))]
    public async Task<IActionResult> SyncShipment([FromRoute] Guid orderId)
    {
        var result = await _mediator.Send(new SyncShipmentCommand { OrderId = orderId });
        return Ok(new ApiOkResponse(result));
    }

    [HttpPost("orders/{orderId:guid}/cancel")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "[GHN][Admin] Cancel an accepted GHN shipment")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(ShipmentSummaryDto))]
    public async Task<IActionResult> CancelShipment([FromRoute] Guid orderId)
    {
        var result = await _mediator.Send(new CancelShipmentCommand { OrderId = orderId });
        return Ok(new ApiOkResponse(result));
    }

    [HttpPatch("orders/{orderId:guid}/note")]
    [Authorize(Roles = nameof(ERole.ADMIN))]
    [SwaggerOperation(Summary = "[GHN][Admin] Update the provider note for a shipment")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully, typeof(ShipmentSummaryDto))]
    public async Task<IActionResult> UpdateShipmentNote([FromRoute] Guid orderId, [FromBody] UpdateShipmentNoteDto dto)
    {
        var result = await _mediator.Send(new UpdateShipmentNoteCommand
        {
            OrderId = orderId,
            Note = dto.Note
        });

        return Ok(new ApiOkResponse(result));
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("Invalid or missing user identity claim.");
        return userId;
    }

    private bool IsAdmin() => User.IsInRole(nameof(ERole.ADMIN));
}
