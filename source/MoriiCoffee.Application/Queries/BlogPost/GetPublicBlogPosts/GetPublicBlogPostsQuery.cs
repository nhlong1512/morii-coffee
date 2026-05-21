using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.BlogPost.GetPublicBlogPosts;

/// <summary>
/// Query for retrieving published blog posts for storefront consumers.
/// </summary>
public class GetPublicBlogPostsQuery : IQuery<Pagination<BlogPostSummaryDto>>
{
    public GetPublicBlogPostsQuery(PaginationFilter filter, string? categorySlug, string? search, string? sort)
    {
        Filter = filter;
        CategorySlug = categorySlug;
        Search = search;
        Sort = sort;
    }

    public PaginationFilter Filter { get; }
    public string? CategorySlug { get; }
    public string? Search { get; }
    public string? Sort { get; }
}
