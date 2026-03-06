using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.Category;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Category.UpdateCategory;

public class UpdateCategoryCommandHandler : ICommandHandler<UpdateCategoryCommand, CategoryDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public UpdateCategoryCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
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

        category.Name = request.Name;
        category.Description = request.Description;
        category.IconUrl = request.IconUrl;
        category.DisplayOrder = request.DisplayOrder;
        category.IsActive = request.IsActive;

        await _unitOfWork.Categories.Update(category);
        await _unitOfWork.CommitAsync();

        return _mapper.Map<CategoryDto>(category);
    }
}
