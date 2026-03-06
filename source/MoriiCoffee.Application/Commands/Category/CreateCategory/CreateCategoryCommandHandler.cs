using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Category.CreateCategory;

/// <summary>Handles the creation of a new product category.</summary>
public class CreateCategoryCommandHandler : ICommandHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public CreateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
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

        var category = new Domain.Aggregates.CategoryAggregate.Category
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            IconUrl = request.IconUrl,
            DisplayOrder = request.DisplayOrder,
            IsActive = true
        };

        await _unitOfWork.Categories.CreateAsync(category);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<CategoryDto>(category);
    }
}
