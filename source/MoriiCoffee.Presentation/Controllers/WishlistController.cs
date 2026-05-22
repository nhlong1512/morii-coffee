using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Commands.Wishlist.AddItemToWishlist;
using MoriiCoffee.Application.Commands.Wishlist.ClearWishlist;
using MoriiCoffee.Application.Commands.Wishlist.MergeGuestWishlist;
using MoriiCoffee.Application.Commands.Wishlist.RemoveItemFromWishlist;
using MoriiCoffee.Application.Queries.Wishlist.GetWishlist;
using MoriiCoffee.Application.SeedWork.DTOs.Wishlist;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>Manages the authenticated user's SQL-backed wishlist.</summary>
[ApiController]
[Route("api/v1/wishlist")]
[Produces("application/json")]
[Authorize]
public class WishlistController : ControllerBase
{
    private readonly IMediator _mediator;

    public WishlistController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Get the current user's wishlist with live product snapshots.</summary>
    [HttpGet]
    [SwaggerOperation(Summary = "Get my wishlist")]
    [SwaggerResponse(200, "Retrieved successfully", typeof(WishlistDto))]
    public async Task<IActionResult> GetWishlist()
    {
        var result = await _mediator.Send(new GetWishlistQuery { UserId = GetCurrentUserId() });
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Add a product to the wishlist. Idempotent — returns 200 if already present.</summary>
    [HttpPost("items")]
    [SwaggerOperation(Summary = "Add item to wishlist")]
    [SwaggerResponse(200, "Item added to wishlist")]
    [SwaggerResponse(404, "Product not found")]
    public async Task<IActionResult> AddItem([FromBody] AddWishlistItemDto dto)
    {
        await _mediator.Send(new AddItemToWishlistCommand
        {
            UserId = GetCurrentUserId(),
            ProductId = dto.ProductId,
        });
        return Ok(new ApiOkResponse("Item added to wishlist."));
    }

    /// <summary>Remove a specific product from the wishlist.</summary>
    [HttpDelete("items/{productId:guid}")]
    [SwaggerOperation(Summary = "Remove item from wishlist")]
    [SwaggerResponse(200, "Item removed")]
    [SwaggerResponse(404, "Item not in wishlist")]
    public async Task<IActionResult> RemoveItem([FromRoute] Guid productId)
    {
        await _mediator.Send(new RemoveItemFromWishlistCommand
        {
            UserId = GetCurrentUserId(),
            ProductId = productId,
        });
        return Ok(new ApiOkResponse("Item removed from wishlist."));
    }

    /// <summary>Merge guest wishlist items into the authenticated user's wishlist on login.</summary>
    [HttpPost("merge")]
    [SwaggerOperation(Summary = "Merge guest wishlist")]
    [SwaggerResponse(200, "Merged wishlist returned", typeof(WishlistDto))]
    public async Task<IActionResult> MergeGuestWishlist([FromBody] MergeGuestWishlistRequestDto dto)
    {
        var result = await _mediator.Send(new MergeGuestWishlistCommand
        {
            UserId = GetCurrentUserId(),
            GuestProductIds = dto.GuestItems.Select(i => i.ProductId).ToList(),
        });
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>Clear all items from the wishlist.</summary>
    [HttpDelete]
    [SwaggerOperation(Summary = "Clear wishlist")]
    [SwaggerResponse(200, "Wishlist cleared")]
    public async Task<IActionResult> ClearWishlist()
    {
        await _mediator.Send(new ClearWishlistCommand { UserId = GetCurrentUserId() });
        return Ok(new ApiOkResponse("Wishlist cleared."));
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

/// <summary>Request body for POST /v1/wishlist/items.</summary>
public class AddWishlistItemDto
{
    public Guid ProductId { get; set; }
}

/// <summary>Request body for POST /v1/wishlist/merge.</summary>
public class MergeGuestWishlistRequestDto
{
    public List<GuestWishlistItemDto> GuestItems { get; set; } = [];
}

public class GuestWishlistItemDto
{
    public Guid ProductId { get; set; }
}
