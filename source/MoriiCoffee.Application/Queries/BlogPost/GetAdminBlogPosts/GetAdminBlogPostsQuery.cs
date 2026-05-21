using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using MoriiCoffee.Domain.Shared.SeedWork;

namespace MoriiCoffee.Application.Queries.BlogPost.GetAdminBlogPosts;

/// <summary>
/// Query for retrieving a paginated admin list of blog posts.
/// </summary>
public class GetAdminBlogPostsQuery : IQuery<Pagination<BlogPostSummaryDto>>
{
    public GetAdminBlogPostsQuery(PaginationFilter filter, EBlogPostStatus? status, Guid? categoryId, string? search)
    {
        Filter = filter;
        Status = status;
        CategoryId = categoryId;
        Search = search;
    }

    public PaginationFilter Filter { get; }
    public EBlogPostStatus? Status { get; }
    public Guid? CategoryId { get; }
    public string? Search { get; }
}
