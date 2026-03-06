using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.Category.GetAllCategories;

/// <summary>Query to retrieve a paginated list of all product categories.</summary>
public record GetAllCategoriesQuery(PaginationFilter Filter) : IQuery<Pagination<CategoryDto>>;
