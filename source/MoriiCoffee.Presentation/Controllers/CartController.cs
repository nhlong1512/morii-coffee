using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Cart.AddItemToCart;
using MoriiCoffee.Application.Commands.Cart.ClearCart;
using MoriiCoffee.Application.Commands.Cart.MergeGuestCart;
using MoriiCoffee.Application.Commands.Cart.RemoveItemFromCart;
using MoriiCoffee.Application.Commands.Cart.UpdateCartItemQuantity;
using MoriiCoffee.Application.Queries.Cart.GetCart;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Manages the authenticated user's Redis-backed shopping cart.
/// All endpoints require a valid JWT — the cart is scoped to the current user.
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

    /// <summary>Get the current user's cart.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get my cart")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(CartDto))]
    public async Task<IActionResult> GetCart()
    {
        var result = await _mediator.Send(new GetCartQuery(GetCurrentUserId()));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Add a product (and optional variant) to the cart. Increments quantity if the item already exists.</summary>
    [HttpPost("items")]
    [SwaggerOperation(Summary = "Add item to cart")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    [SwaggerResponse(404, SwaggerResponseMessages.NotFound)]
    public async Task<IActionResult> AddItem([FromBody] AddCartItemDto dto)
    {
        await _mediator.Send(new AddItemToCartCommand
        {
            UserId = GetCurrentUserId(),
            ProductId = dto.ProductId,
            VariantId = dto.VariantId,
            Quantity = dto.Quantity
        });
        return Ok(new ApiOkResponse("Item added to cart."));
    }

    /// <summary>Set the exact quantity of a cart item. Sending quantity 0 removes the item.</summary>
    [HttpPut("items")]
    [SwaggerOperation(Summary = "Update cart item quantity")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> UpdateItemQuantity([FromBody] UpdateCartItemQuantityDto dto)
    {
        await _mediator.Send(new UpdateCartItemQuantityCommand
        {
            UserId = GetCurrentUserId(),
            ProductId = dto.ProductId,
            VariantId = dto.VariantId,
            Quantity = dto.Quantity
        });
        return Ok(new ApiOkResponse("Cart item quantity updated."));
    }

    /// <summary>Remove a specific product/variant combination from the cart.</summary>
    [HttpDelete("items")]
    [SwaggerOperation(Summary = "Remove item from cart")]
    [SwaggerResponse(200, SwaggerResponseMessages.DeletedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> RemoveItem([FromBody] RemoveCartItemDto dto)
    {
        await _mediator.Send(new RemoveItemFromCartCommand
        {
            UserId = GetCurrentUserId(),
            ProductId = dto.ProductId,
            VariantId = dto.VariantId
        });
        return Ok(new ApiOkResponse("Item removed from cart."));
    }

    /// <summary>Remove all items from the cart.</summary>
    [HttpDelete]
    [SwaggerOperation(Summary = "Clear cart")]
    [SwaggerResponse(200, SwaggerResponseMessages.DeletedSuccessfully)]
    public async Task<IActionResult> ClearCart()
    {
        await _mediator.Send(new ClearCartCommand { UserId = GetCurrentUserId() });
        return Ok(new ApiOkResponse("Cart cleared."));
    }

    /// <summary>
    /// Merge localStorage guest cart into the authenticated user's Redis cart.
    /// Call this immediately after login. Items with the same product/variant have quantities summed.
    /// </summary>
    [HttpPost("merge")]
    [SwaggerOperation(Summary = "Merge guest cart after login")]
    [SwaggerResponse(200, SwaggerResponseMessages.UpdatedSuccessfully)]
    [SwaggerResponse(400, SwaggerResponseMessages.BadRequest)]
    public async Task<IActionResult> MergeGuestCart([FromBody] MergeGuestCartDto dto)
    {
        await _mediator.Send(new MergeGuestCartCommand
        {
            UserId = GetCurrentUserId(),
            GuestItems = dto.GuestItems
        });
        return Ok(new ApiOkResponse("Guest cart merged."));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private Guid GetCurrentUserId()
    {
        var sub = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(sub, out var userId))
            throw new UnauthorizedException("Invalid or missing user identity claim.");
        return userId;
    }
}
