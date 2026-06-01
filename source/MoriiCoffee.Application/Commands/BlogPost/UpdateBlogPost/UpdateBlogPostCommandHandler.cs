using AutoMapper;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.BlogPost.UpdateBlogPost;

/// <summary>
/// Handles full updates to an existing blog post, including tracked category reassignment.
/// </summary>
public class UpdateBlogPostCommandHandler : ICommandHandler<UpdateBlogPostCommand, BlogPostDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateBlogPostCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BlogPostDetailDto> Handle(UpdateBlogPostCommand request, CancellationToken cancellationToken)
    {
        var post = await _unitOfWork.BlogPosts
            .FindByCondition(x => x.Id == request.Id, true)
            .Include(x => x.BlogPostCategories)
                .ThenInclude(x => x.BlogCategory)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new NotFoundException("BlogPost", request.Id);

        string slug = string.IsNullOrWhiteSpace(request.Slug)
            ? CreateBlogPost.CreateBlogPostCommandHandler.GenerateSlug(request.Title)
            : CreateBlogPost.CreateBlogPostCommandHandler.GenerateSlug(request.Slug);

        if (await _unitOfWork.BlogPosts.SlugExistsAsync(slug, request.Id))
            throw new BadRequestException($"The slug '{slug}' is already in use by another blog post.");

        foreach (var categoryId in request.CategoryIds.Distinct())
        {
            _ = await _unitOfWork.BlogCategories.GetByIdAsync(categoryId)
                ?? throw new NotFoundException("BlogCategory", categoryId);
        }

        CreateBlogPost.CreateBlogPostCommandHandler.ValidatePublishReady(
            request.Status,
            request.ContentHtml,
            request.ContentJson,
            request.CategoryIds);

        post.Update(
            request.Title,
            slug,
            request.Excerpt,
            request.ContentJson,
            request.ContentHtml,
            request.CoverImageUrl,
            request.CoverImageFileName,
            request.SeoTitle,
            request.SeoDescription,
            request.IsFeatured,
            request.DisplayOrder,
            request.Status);

        post.ReplaceCategories(request.CategoryIds);

        await _unitOfWork.BlogPosts.Update(post);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BlogPostDetailDto>(post);
    }
}
