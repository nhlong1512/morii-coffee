using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using BlogCategoryEntity = MoriiCoffee.Domain.Aggregates.BlogCategoryAggregate.BlogCategory;

namespace MoriiCoffee.Application.Commands.BlogCategory.CreateBlogCategory;

/// <summary>
/// Handles creation of a new blog category with normalized unique slug generation.
/// </summary>
public class CreateBlogCategoryCommandHandler : ICommandHandler<CreateBlogCategoryCommand, BlogCategoryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateBlogCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BlogCategoryDto> Handle(CreateBlogCategoryCommand request, CancellationToken cancellationToken)
    {
        bool slugProvided = !string.IsNullOrWhiteSpace(request.Slug);
        string slug = GenerateSlug(slugProvided ? request.Slug! : request.Name);

        if (await _unitOfWork.BlogCategories.SlugExistsAsync(slug))
        {
            if (slugProvided)
                throw new BadRequestException($"The slug '{slug}' is already in use by another blog category.");

            slug = $"{slug}-{Guid.NewGuid().ToString("N")[..6]}";
        }

        var category = BlogCategoryEntity.Create(
            request.Name,
            slug,
            request.Description,
            request.DisplayOrder,
            request.IsActive);

        await _unitOfWork.BlogCategories.CreateAsync(category);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BlogCategoryDto>(category);
    }

    internal static string GenerateSlug(string value)
    {
        var slug = SlugHelper.Generate(value);

        return string.IsNullOrWhiteSpace(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
