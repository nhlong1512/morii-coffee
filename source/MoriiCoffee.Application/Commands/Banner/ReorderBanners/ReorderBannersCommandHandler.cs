using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Banner.ReorderBanners;

/// <summary>
/// Bulk-updates the DisplayOrder of the supplied banners.
/// Each banner is fetched individually; missing IDs throw NotFoundException.
/// </summary>
public class ReorderBannersCommandHandler : ICommandHandler<ReorderBannersCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;

    public ReorderBannersCommandHandler(IUnitOfWork unitOfWork) => _unitOfWork = unitOfWork;

    public async Task<bool> Handle(ReorderBannersCommand request, CancellationToken cancellationToken)
    {
        foreach (var item in request.Items)
        {
            var banner = await _unitOfWork.Banners.GetByIdAsync(item.Id)
                ?? throw new NotFoundException("Banner", item.Id);

            banner.Reorder(item.DisplayOrder);
            await _unitOfWork.Banners.Update(banner);
        }

        await _unitOfWork.CommitAsync();
        return true;
    }
}
