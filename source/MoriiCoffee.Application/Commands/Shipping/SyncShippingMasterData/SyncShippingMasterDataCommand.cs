using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Command;

namespace MoriiCoffee.Application.Commands.Shipping.SyncShippingMasterData;

public class SyncShippingMasterDataCommand : ICommand<ShippingMasterDataSyncResultDto>
{
    public int? ProvinceId { get; set; }

    public int? DistrictId { get; set; }
}
