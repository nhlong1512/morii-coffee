using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Product.DeleteProduct;

/// <summary>Soft-deletes a product and invalidates its catalog cache entries.</summary>
public class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IProductCatalogCache _catalogCache;

    public DeleteProductCommandHandler(IUnitOfWork unitOfWork, IProductCatalogCache catalogCache)
    {
        _unitOfWork = unitOfWork;
        _catalogCache = catalogCache;
    }

    public async Task<bool> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _unitOfWork.Products.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Product", request.Id);

        await _unitOfWork.Products.SoftDelete(product);
        await _unitOfWork.CommitAsync();

        await _catalogCache.InvalidateProductAsync(request.Id);
        await _catalogCache.InvalidateAllListsAsync();

        return true;
    }
}
