using MoriiCoffee.Application.SeedWork.DTOs.ProductVariant;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.ProductVariant.GetVariantById;

/// <summary>Query to retrieve a single product variant by its ID.</summary>
public record GetVariantByIdQuery(Guid VariantId) : IQuery<ProductVariantDto>;
