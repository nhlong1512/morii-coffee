using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Queries.BlogPost.GetPublicBlogPosts;

/// <summary>
/// Returns a paginated list of published blog posts for the public site.
/// </summary>
public class GetPublicBlogPostsQueryHandler : IQueryHandler<GetPublicBlogPostsQuery, Pagination<BlogPostSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublicBlogPostsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Pagination<BlogPostSummaryDto>> Handle(GetPublicBlogPostsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<BlogPostEntity> query = _unitOfWork.BlogPosts
            .FindAll(false)
            .Include(x => x.BlogPostCategories)
                .ThenInclude(x => x.BlogCategory)
            .Where(x => x.Status == EBlogPostStatus.Published);

        if (!string.IsNullOrWhiteSpace(request.CategorySlug))
        {
            var categorySlug = request.CategorySlug.Trim().ToLowerInvariant();
            query = query.Where(x => x.BlogPostCategories.Any(c =>
                !c.IsDeleted &&
                !c.BlogCategory.IsDeleted &&
                c.BlogCategory.IsActive &&
                c.BlogCategory.Slug == categorySlug));
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search) ||
                (x.Excerpt != null && x.Excerpt.ToLower().Contains(search)));
        }

        query = ApplySort(query, request.Sort);

        var dtoQuery = query
            .AsEnumerable()
            .Select(x => _mapper.Map<BlogPostSummaryDto>(x))
            .AsQueryable();

        return Task.FromResult(PagingHelper.QueryPaginate(request.Filter, dtoQuery));
    }

    private static IQueryable<BlogPostEntity> ApplySort(IQueryable<BlogPostEntity> query, string? sort)
    {
        return sort?.Trim().ToLowerInvariant() switch
        {
            "publishedat_asc" => query.OrderBy(x => x.PublishedAt).ThenBy(x => x.DisplayOrder),
            "title_asc" => query.OrderBy(x => x.Title).ThenBy(x => x.DisplayOrder),
            "title_desc" => query.OrderByDescending(x => x.Title).ThenBy(x => x.DisplayOrder),
            _ => query.OrderBy(x => x.DisplayOrder).ThenByDescending(x => x.PublishedAt).ThenByDescending(x => x.CreatedAt)
        };
    }
}
