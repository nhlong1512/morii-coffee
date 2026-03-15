using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Category.UpdateCategory;

/// <summary>
/// Handles updating an existing product category.
/// When a new icon file is provided, the old MinIO object is deleted first,
/// then the new file is uploaded before the DB commit.
/// </summary>
public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(UpdateCategoryCommand request, CancellationToken cancellationToken)
    {
        var category = await _unitOfWork.Categories.GetByIdAsync(request.Id)
            ?? throw new NotFoundException(nameof(Domain.Aggregates.CategoryAggregate.Category), request.Id);

        // Check for name conflict with other categories
        var existing = await _unitOfWork.Categories.GetByNameAsync(request.Name);
        if (existing != null && existing.Id != request.Id)
        {
            throw new BadRequestException($"A category with the name '{request.Name}' already exists.");
        }

        // Replace icon: delete old from MinIO, upload new
        if (request.Icon != null)
        {
            if (!string.IsNullOrEmpty(category.IconFileName))
                await _fileService.DeleteAsync(FileContainers.CATEGORIES, category.IconFileName);

            var uploadResult = await _fileService.UploadAsync(request.Icon, FileContainers.CATEGORIES);
            category.IconUrl = uploadResult.Blob.Uri;
            category.IconFileName = uploadResult.Blob.Name;
        }

        category.Name = request.Name;
        category.Description = request.Description;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        await _unitOfWork.Categories.Update(category);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<CategoryDto>(category);
    }
}
