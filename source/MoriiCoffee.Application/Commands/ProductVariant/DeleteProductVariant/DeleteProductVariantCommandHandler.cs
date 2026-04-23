using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.ProductVariant.DeleteProductVariant;

/// <summary>Soft-deletes a product variant and invalidates related catalog cache entries.</summary>
public class DeleteProductVariantCommandHandler : ICommandHandler<DeleteProductVariantCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductCatalogCache _catalogCache;

    public DeleteProductVariantCommandHandler(IUnitOfWork unitOfWork, IProductCatalogCache catalogCache)
    {
        _unitOfWork = unitOfWork;
        _catalogCache = catalogCache;
    }

    public async Task<bool> Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _unitOfWork.ProductVariants.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("ProductVariant", request.Id);

        var productId = variant.ProductId;

        await _unitOfWork.ProductVariants.SoftDelete(variant);
        await _unitOfWork.CommitAsync();

        await _catalogCache.InvalidateProductAsync(productId);
        await _catalogCache.InvalidateAllListsAsync();

        return true;
    }
}
