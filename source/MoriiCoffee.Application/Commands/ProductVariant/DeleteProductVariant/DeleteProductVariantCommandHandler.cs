using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.ProductVariant.DeleteProductVariant;

public class DeleteProductVariantCommandHandler : ICommandHandler<DeleteProductVariantCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteProductVariantCommandHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> Handle(DeleteProductVariantCommand request, CancellationToken cancellationToken)
    {
        var variant = await _unitOfWork.ProductVariants.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("ProductVariant", request.Id);

        await _unitOfWork.ProductVariants.SoftDelete(variant);
        await _unitOfWork.CommitAsync();

        return true;
    }
}
