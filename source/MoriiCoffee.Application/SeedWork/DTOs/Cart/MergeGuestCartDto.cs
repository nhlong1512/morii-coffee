namespace MoriiCoffee.Application.SeedWork.DTOs.Cart;

/// <summary>Request body carrying the guest cart items to merge into the authenticated user's cart after login.</summary>
public class MergeGuestCartDto
{
    /// <summary>Items from localStorage guest cart to merge.</summary>
    public List<CartItemDto> GuestItems { get; set; } = [];
}
