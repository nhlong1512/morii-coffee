using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Shipping.SyncShippingMasterData;

public class SyncShippingMasterDataCommandHandler : ICommandHandler<SyncShippingMasterDataCommand, ShippingMasterDataSyncResultDto>
{
    private readonly IShippingGateway _shippingGateway;
    private readonly IUnitOfWork _unitOfWork;

    public SyncShippingMasterDataCommandHandler(IShippingGateway shippingGateway, IUnitOfWork unitOfWork)
    {
        _shippingGateway = shippingGateway;
        _unitOfWork = unitOfWork;
    }

    public async Task<ShippingMasterDataSyncResultDto> Handle(SyncShippingMasterDataCommand request, CancellationToken cancellationToken)
    {
        if (request.DistrictId.HasValue)
        {
            var wards = await _shippingGateway.GetWardsAsync(request.DistrictId.Value, cancellationToken);
            await _unitOfWork.ShippingMasterData.UpsertWardsAsync(
                wards.Select(x => ShippingWard.Create(x.WardCode, x.DistrictId, x.WardName)),
                cancellationToken);
            await _unitOfWork.CommitAsync();

            return new ShippingMasterDataSyncResultDto
            {
                Scope = $"wards:{request.DistrictId.Value}",
                SyncedCount = wards.Count
            };
        }

        if (request.ProvinceId.HasValue)
        {
            var districts = await _shippingGateway.GetDistrictsAsync(request.ProvinceId.Value, cancellationToken);
            await _unitOfWork.ShippingMasterData.UpsertDistrictsAsync(
                districts.Select(x => ShippingDistrict.Create(x.DistrictId, x.ProvinceId, x.DistrictName, x.SupportType)),
                cancellationToken);
            await _unitOfWork.CommitAsync();

            return new ShippingMasterDataSyncResultDto
            {
                Scope = $"districts:{request.ProvinceId.Value}",
                SyncedCount = districts.Count
            };
        }

        var provinces = await _shippingGateway.GetProvincesAsync(cancellationToken);
        await _unitOfWork.ShippingMasterData.UpsertProvincesAsync(
            provinces.Select(x => ShippingProvince.Create(x.ProvinceId, x.ProvinceName, x.Code)),
            cancellationToken);
        await _unitOfWork.CommitAsync();

        return new ShippingMasterDataSyncResultDto
        {
            Scope = "provinces",
            SyncedCount = provinces.Count
        };
    }
}
