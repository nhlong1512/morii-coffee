using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.BlogCategory.GetAdminBlogCategories;

/// <summary>
/// Query for retrieving paginated blog categories in the admin area.
/// </summary>
public class GetAdminBlogCategoriesQuery : IQuery<Pagination<BlogCategoryDto>>
{
    public GetAdminBlogCategoriesQuery(PaginationFilter filter, string? search)
    {
        Filter = filter;
        Search = search;
    }

    public PaginationFilter Filter { get; }
    public string? Search { get; }
}
