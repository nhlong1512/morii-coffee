using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Banner.DeleteBanner;

/// <summary>Soft-deletes a banner. The record is retained in the database with IsDeleted = true.</summary>
public class DeleteBannerCommandHandler : ICommandHandler<DeleteBannerCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBannerCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(DeleteBannerCommand request, CancellationToken cancellationToken)
    {
        var banner = await _unitOfWork.Banners.GetByIdAsync(request.Id)
            ?? throw new NotFoundException("Banner", request.Id);

        await _unitOfWork.Banners.SoftDelete(banner);
        await _unitOfWork.CommitAsync();

        return true;
    }
}
