using AutoMapper;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;

namespace MoriiCoffee.Application.Commands.Category.CreateCategory;

/// <summary>
/// Handles the creation of a new product category.
/// If an icon file is provided, it is uploaded to MinIO before the DB commit.
/// </summary>
public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IFileService _fileService;
    private readonly IMapper _mapper;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork, IFileService fileService, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _fileService = fileService;
        _mapper = mapper;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken cancellationToken)
    {
        // Ensure uniqueness of category name
        var existing = await _unitOfWork.Categories.GetByNameAsync(request.Name);
        if (existing != null)
        {
            throw new BadRequestException($"A category with the name '{request.Name}' already exists.");
        }

        // Upload icon to MinIO if provided
        string? iconUrl = null;
        string? iconFileName = null;
        if (request.Icon != null)
        {
            var uploadResult = await _fileService.UploadAsync(request.Icon, FileContainers.CATEGORIES);
            iconUrl = uploadResult.Blob.Uri;
            iconFileName = uploadResult.Blob.Name;
        }

        var category = new Domain.Aggregates.CategoryAggregate.Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IconUrl = iconUrl,
            IconFileName = iconFileName,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        await _unitOfWork.Categories.CreateAsync(category);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<CategoryDto>(category);
    }
}
