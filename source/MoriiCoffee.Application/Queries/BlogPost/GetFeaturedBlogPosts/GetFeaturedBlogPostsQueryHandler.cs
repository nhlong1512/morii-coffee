using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.Blog;

namespace MoriiCoffee.Application.Queries.BlogPost.GetFeaturedBlogPosts;

/// <summary>
/// Returns featured blog posts that are publicly visible.
/// </summary>
public class GetFeaturedBlogPostsQueryHandler : IQueryHandler<GetFeaturedBlogPostsQuery, List<BlogPostSummaryDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetFeaturedBlogPostsQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<BlogPostSummaryDto>> Handle(GetFeaturedBlogPostsQuery request, CancellationToken cancellationToken)
    {
        var take = request.Take <= 0 ? 3 : request.Take;

        var posts = await _unitOfWork.BlogPosts
            .FindByCondition(x => x.Status == EBlogPostStatus.Published && x.IsFeatured, false)
            .Include(x => x.BlogPostCategories)
                .ThenInclude(x => x.BlogCategory)
            .OrderBy(x => x.DisplayOrder)
            .ThenByDescending(x => x.PublishedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        return posts.Select(x => _mapper.Map<BlogPostSummaryDto>(x)).ToList();
    }
}
