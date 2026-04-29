using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Cart.GetCart;

/// <summary>Query to retrieve the current cart state for the authenticated user.</summary>
public record GetCartQuery(Guid UserId) : IQuery<CartDto>;
