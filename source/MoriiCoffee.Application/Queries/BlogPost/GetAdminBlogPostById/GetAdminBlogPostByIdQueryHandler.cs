using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.BlogPost.GetAdminBlogPostById;

/// <summary>
/// Returns one admin-visible blog post with full editable detail.
/// </summary>
public class GetAdminBlogPostByIdQueryHandler : IQueryHandler<GetAdminBlogPostByIdQuery, BlogPostDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public GetAdminBlogPostByIdQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BlogPostDetailDto> Handle(GetAdminBlogPostByIdQuery request, CancellationToken cancellationToken)
    {
        var post = await _unitOfWork.BlogPosts
            .FindByCondition(x => x.Id == request.BlogPostId, false)
            .Include(x => x.BlogPostCategories)
                .ThenInclude(x => x.BlogCategory)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("BlogPost", request.BlogPostId);

        return _mapper.Map<BlogPostDetailDto>(post);
    }
}
