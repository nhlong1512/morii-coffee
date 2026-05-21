using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Blog;
using System.Text.RegularExpressions;
using BlogPostEntity = MoriiCoffee.Domain.Aggregates.BlogPostAggregate.BlogPost;

namespace MoriiCoffee.Application.Commands.BlogPost.CreateBlogPost;

/// <summary>
/// Handles creation of a new blog post including slug normalization and category validation.
/// </summary>
public class CreateBlogPostCommandHandler : ICommandHandler<CreateBlogPostCommand, BlogPostDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateBlogPostCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BlogPostDetailDto> Handle(CreateBlogPostCommand request, CancellationToken cancellationToken)
    {
        bool slugProvided = !string.IsNullOrWhiteSpace(request.Slug);
        string slug = GenerateSlug(slugProvided ? request.Slug! : request.Title);

        if (await _unitOfWork.BlogPosts.SlugExistsAsync(slug))
        {
            if (slugProvided)
                throw new BadRequestException($"The slug '{slug}' is already in use by another blog post.");

            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
        }

        await ValidateCategoriesAsync(request.CategoryIds);
        ValidatePublishReady(request.Status, request.ContentHtml, request.ContentJson, request.CategoryIds);

        var post = BlogPostEntity.Create(
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

        await _unitOfWork.BlogPosts.CreateAsync(post);
        await _unitOfWork.CommitAsync();

        var created = await _unitOfWork.BlogPosts.GetByIdAsync(post.Id,
            x => x.BlogPostCategories)
            ?? throw new NotFoundException("BlogPost", post.Id);

        return _mapper.Map<BlogPostDetailDto>(created);
    }

    private async Task ValidateCategoriesAsync(IEnumerable<Guid> categoryIds)
    {
        foreach (var categoryId in categoryIds.Distinct())
        {
            var category = await _unitOfWork.BlogCategories.GetByIdAsync(categoryId)
                ?? throw new NotFoundException("BlogCategory", categoryId);

            if (category.IsDeleted)
                throw new BadRequestException($"Blog category '{categoryId}' is not available.");
        }
    }

    internal static void ValidatePublishReady(
        EBlogPostStatus status,
        string contentHtml,
        string? contentJson,
        IReadOnlyCollection<Guid> categoryIds)
    {
        if (status != EBlogPostStatus.Published)
            return;

        if (string.IsNullOrWhiteSpace(contentHtml))
            throw new BadRequestException("Published blog posts must have HTML content.");

        if (string.IsNullOrWhiteSpace(contentJson))
            throw new BadRequestException("Published blog posts must have editor JSON content.");

        if (categoryIds.Count == 0)
            throw new BadRequestException("Published blog posts must have at least one category.");
    }

    internal static string GenerateSlug(string value)
    {
        var slug = Regex.Replace(value.ToLowerInvariant().Trim(), @"[^a-z0-9\s-]", string.Empty)
            .Replace(" ", "-")
            .Trim('-');

        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
