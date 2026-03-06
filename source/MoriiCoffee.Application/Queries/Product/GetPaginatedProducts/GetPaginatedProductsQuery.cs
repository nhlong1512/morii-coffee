using MoriiCoffee.Application.SeedWork.DTOs.Product;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Product.GetPaginatedProducts;

/// <summary>Query to retrieve a paginated list of products with optional filtering.</summary>
public record GetPaginatedProductsQuery(ProductPaginationFilter Filter) : IQuery<Pagination<ProductSummaryDto>>;
