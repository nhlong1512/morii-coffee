using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Product.GetProductById;

/// <summary>Query to retrieve a single product with all its variants by ID.</summary>
public record GetProductByIdQuery(Guid ProductId) : IQuery<ProductDto>;
