using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Category.GetCategoryById;

/// <summary>Query to retrieve a single category by its ID.</summary>
public record GetCategoryByIdQuery(Guid CategoryId) : IQuery<CategoryDto>;
