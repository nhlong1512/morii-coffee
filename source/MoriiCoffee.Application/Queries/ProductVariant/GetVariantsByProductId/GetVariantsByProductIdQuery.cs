using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.ProductVariant.GetVariantsByProductId;

/// <summary>Query to retrieve all variants for a specific product.</summary>
public record GetVariantsByProductIdQuery(Guid ProductId) : IQuery<IEnumerable<ProductVariantDto>>;
