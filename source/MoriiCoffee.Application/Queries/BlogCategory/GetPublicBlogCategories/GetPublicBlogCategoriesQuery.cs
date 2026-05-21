using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.BlogCategory.GetPublicBlogCategories;

/// <summary>
/// Query for retrieving blog categories available to public consumers.
/// </summary>
public record GetPublicBlogCategoriesQuery(bool ActiveOnly = true) : IQuery<List<BlogCategoryDto>>;
