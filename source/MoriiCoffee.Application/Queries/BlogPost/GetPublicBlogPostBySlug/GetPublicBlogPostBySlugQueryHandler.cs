using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Enums.Blog;

namespace MoriiCoffee.Application.Queries.BlogPost.GetPublicBlogPostBySlug;

/// <summary>
/// Returns a single published blog post by slug for storefront detail pages.
/// </summary>
public class GetPublicBlogPostBySlugQueryHandler : IQueryHandler<GetPublicBlogPostBySlugQuery, BlogPostDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetPublicBlogPostBySlugQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BlogPostDetailDto> Handle(GetPublicBlogPostBySlugQuery request, CancellationToken cancellationToken)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        var post = await _unitOfWork.BlogPosts
            .FindByCondition(x => x.Slug == slug && x.Status == EBlogPostStatus.Published, false)
            .Include(x => x.BlogPostCategories)
                .ThenInclude(x => x.BlogCategory)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("BlogPost", request.Slug);

        return _mapper.Map<BlogPostDetailDto>(post);
    }
}
