using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Shipping.GetShippingWards;

public class GetShippingWardsQueryHandler : IQueryHandler<GetShippingWardsQuery, List<ShippingWardDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShippingGateway _shippingGateway;

    public GetShippingWardsQueryHandler(IUnitOfWork unitOfWork, IShippingGateway shippingGateway)
    {
        _unitOfWork = unitOfWork;
        _shippingGateway = shippingGateway;
    }

    public async Task<List<ShippingWardDto>> Handle(GetShippingWardsQuery request, CancellationToken cancellationToken)
    {
        var wards = await _unitOfWork.ShippingMasterData.GetWardsByDistrictIdAsync(request.DistrictId, cancellationToken);
        if (wards.Count == 0)
        {
            var providerWards = await _shippingGateway.GetWardsAsync(request.DistrictId, cancellationToken);
            var normalized = providerWards
                .Select(x => ShippingWard.Create(x.WardCode, x.DistrictId, x.WardName))
                .ToList();

            await _unitOfWork.ShippingMasterData.UpsertWardsAsync(normalized, cancellationToken);
            await _unitOfWork.CommitAsync();
            wards = normalized;
        }

        return wards.Select(x => new ShippingWardDto
        {
            WardCode = x.WardCode,
            DistrictId = x.DistrictId,
            WardName = x.WardName
        }).ToList();
    }
}
