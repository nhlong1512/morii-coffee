using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Helpers;
using MoriiCoffee.Domain.Shared.SeedWork;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Queries.BlogPost.GetAdminBlogPosts;

/// <summary>
/// Returns a paginated admin-focused list of blog posts including unpublished content.
/// </summary>
public class GetAdminBlogPostsQueryHandler : IQueryHandler<GetAdminBlogPostsQuery, Pagination<BlogPostSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAdminBlogPostsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public Task<Pagination<BlogPostSummaryDto>> Handle(GetAdminBlogPostsQuery request, CancellationToken cancellationToken)
    {
        IQueryable<BlogPostEntity> query = _unitOfWork.BlogPosts
            .FindAll(false)
            .Include(x => x.BlogPostCategories)
                .ThenInclude(x => x.BlogCategory);

        if (request.Status.HasValue)
            query = query.Where(x => x.Status == request.Status.Value);

        if (request.CategoryId.HasValue)
            query = query.Where(x => x.BlogPostCategories.Any(c => c.BlogCategoryId == request.CategoryId.Value && !c.IsDeleted));

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim().ToLowerInvariant();
            query = query.Where(x =>
                x.Title.ToLower().Contains(search) ||
                (x.Excerpt != null && x.Excerpt.ToLower().Contains(search)));
        }

        var dtoQuery = query
            .OrderByDescending(x => x.UpdatedAt ?? x.CreatedAt)
            .ThenBy(x => x.DisplayOrder)
            .AsEnumerable()
            .Select(x => _mapper.Map<BlogPostSummaryDto>(x))
            .AsQueryable();

        return Task.FromResult(PagingHelper.QueryPaginate(request.Filter, dtoQuery));
    }
}
