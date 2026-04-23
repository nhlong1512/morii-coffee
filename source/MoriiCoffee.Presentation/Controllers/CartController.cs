using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Cart.AddCartItem;
using MoriiCoffee.Application.Commands.Cart.ClearCart;
using MoriiCoffee.Application.Commands.Cart.RemoveCartItem;
using MoriiCoffee.Application.Commands.Cart.UpdateCartItem;
using MoriiCoffee.Application.Queries.Cart.GetCart;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Authenticated-user cart endpoints.
/// All operations require a valid JWT bearer token.
/// Returns 503 when the Redis cart store is unavailable.
/// </summary>
[ApiController]
[Route("api/v1/cart")]
[Produces("application/json")]
[Authorize]
public class CartController : ControllerBase
{
    private readonly IMediator _mediator;

    public CartController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get the authenticated user's active cart.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get cart", Description = "Returns the active cart for the authenticated user, or an empty cart if none exists.")]
    [SwaggerResponse(200, "Cart retrieved successfully.", typeof(CartDto))]
    [SwaggerResponse(401, "Authentication required.")]
    [SwaggerResponse(503, "Cart storage unavailable.")]
    public async Task<IActionResult> GetCart()
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new GetCartQuery(userId));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Add a product variant to the cart.</summary>
    [HttpPost("items")]
    [SwaggerOperation(Summary = "Add cart item", Description = "Adds a variant to the cart. If the variant is already in the cart, the quantity is accumulated.")]
    [SwaggerResponse(200, "Cart updated successfully.", typeof(CartDto))]
    [SwaggerResponse(400, "Invalid variant or quantity.")]
    [SwaggerResponse(401, "Authentication required.")]
    [SwaggerResponse(404, "Product variant not found.")]
    [SwaggerResponse(503, "Cart storage unavailable.")]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new AddCartItemCommand
        {
            UserId = userId,
            VariantId = dto.VariantId,
            Quantity = dto.Quantity
        });
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Update quantity for an existing cart line.</summary>
    [HttpPut("items/{variantId:guid}")]
    [SwaggerOperation(Summary = "Update cart item", Description = "Updates the quantity for a cart line. Pass quantity 0 to remove the line.")]
    [SwaggerResponse(200, "Cart updated successfully.", typeof(CartDto))]
    [SwaggerResponse(400, "Invalid quantity.")]
    [SwaggerResponse(401, "Authentication required.")]
    [SwaggerResponse(404, "Cart line not found.")]
    [SwaggerResponse(503, "Cart storage unavailable.")]
    public async Task<IActionResult> UpdateItem(Guid variantId, [FromBody] UpdateCartItemDto dto)
    {
        var userId = GetCurrentUserId();
        var result = await _mediator.Send(new UpdateCartItemCommand
        {
            UserId = userId,
            VariantId = variantId,
            Quantity = dto.Quantity
        });
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Remove a product variant line from the cart.</summary>
    [HttpDelete("items/{variantId:guid}")]
    [SwaggerOperation(Summary = "Remove cart item", Description = "Removes a single variant line from the cart.")]
    [SwaggerResponse(204, "Cart line removed.")]
    [SwaggerResponse(401, "Authentication required.")]
    [SwaggerResponse(404, "Cart line not found.")]
    [SwaggerResponse(503, "Cart storage unavailable.")]
    public async Task<IActionResult> RemoveItem(Guid variantId)
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new RemoveCartItemCommand { UserId = userId, VariantId = variantId });
        return NoContent();
    }

    /// <summary>Clear all items from the cart.</summary>
    [HttpDelete]
    [SwaggerOperation(Summary = "Clear cart", Description = "Deletes the entire cart for the authenticated user.")]
    [SwaggerResponse(204, "Cart cleared.")]
    [SwaggerResponse(401, "Authentication required.")]
    [SwaggerResponse(503, "Cart storage unavailable.")]
    public async Task<IActionResult> ClearCart()
    {
        var userId = GetCurrentUserId();
        await _mediator.Send(new ClearCartCommand { UserId = userId });
        return NoContent();
    }

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("Invalid or missing user identity claim.");
        return userId;
    }
}
