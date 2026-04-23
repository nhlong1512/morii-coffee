using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Cart.GetCart;

/// <summary>Returns the authenticated user's active cart.</summary>
public record GetCartQuery(Guid UserId) : IQuery<CartDto>;
