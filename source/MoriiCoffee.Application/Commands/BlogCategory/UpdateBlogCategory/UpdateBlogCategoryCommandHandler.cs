using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Blog;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.BlogCategory.UpdateBlogCategory;

/// <summary>
/// Handles updates to an existing blog category.
/// </summary>
public class UpdateBlogCategoryCommandHandler : ICommandHandler<UpdateBlogCategoryCommand, BlogCategoryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateBlogCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<BlogCategoryDto> Handle(UpdateBlogCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.BlogCategories.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("BlogCategory", request.Id);

        string slug = string.IsNullOrWhiteSpace(request.Slug)
            ? CreateBlogCategory.CreateBlogCategoryCommandHandler.GenerateSlug(request.Name)
            : CreateBlogCategory.CreateBlogCategoryCommandHandler.GenerateSlug(request.Slug);

        if (await _unitOfWork.BlogCategories.SlugExistsAsync(slug, request.Id))
            throw new BadRequestException($"The slug '{slug}' is already in use by another blog category.");

        category.Update(request.Name, slug, request.Description, request.DisplayOrder, request.IsActive);

        await _unitOfWork.BlogCategories.Update(category);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<BlogCategoryDto>(category);
    }
}
