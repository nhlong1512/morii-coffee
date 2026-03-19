using AutoMapper;
using MoriiCoffee.Application.SeedWork.DTOs.ProductImage;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Product.ReorderProductImages;

/// <summary>
/// Reassigns <c>DisplayOrder</c> values to a product's gallery images based on the
/// caller-supplied ordering. The first ID in the list becomes order 1, the second order 2, etc.
/// All IDs must belong to the specified product.
/// </summary>
public class ReorderProductImagesCommandHandler : ICommandHandler<ReorderProductImagesCommand, List<ProductImageDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IMapper _mapper;

    public ReorderProductImagesCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
    {
        _unitOfWork = unitOfWork;
        _mapper = mapper;
    }

    public async Task<List<ProductImageDto>> Handle(
        ReorderProductImagesCommand request,
        CancellationToken cancellationToken)
    {
        var existing = (await _unitOfWork.ProductImages.GetByProductIdAsync(request.ProductId))
            .ToDictionary(i => i.Id);

        if (existing.Count == 0)
            throw new NotFoundException("Product images", request.ProductId);

        // Validate: all requested IDs must belong to this product
        var unknownIds = request.ImageIds.Where(id => !existing.ContainsKey(id)).ToList();
        if (unknownIds.Count > 0)
            throw new BadRequestException(
                $"The following image IDs do not belong to this product: {string.Join(", ", unknownIds)}");

        // Validate: the list must include every existing image
        if (request.ImageIds.Count != existing.Count)
            throw new BadRequestException(
                $"The reorder list must include all {existing.Count} images for this product. " +
                $"{request.ImageIds.Count} were provided.");

        // Apply new display order
        for (int i = 0; i < request.ImageIds.Count; i++)
        {
            var image = existing[request.ImageIds[i]];
            image.DisplayOrder = i + 1;
            await _unitOfWork.ProductImages.Update(image);
        }

        await _unitOfWork.CommitAsync();

        return existing.Values
            .OrderBy(i => i.DisplayOrder)
            .Select(i => _mapper.Map<ProductImageDto>(i))
            .ToList();
    }
}
