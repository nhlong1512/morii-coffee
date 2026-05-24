using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Shipping.GetShippingDistricts;

public class GetShippingDistrictsQueryHandler : IQueryHandler<GetShippingDistrictsQuery, List<ShippingDistrictDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShippingGateway _shippingGateway;

    public GetShippingDistrictsQueryHandler(IUnitOfWork unitOfWork, IShippingGateway shippingGateway)
    {
        _unitOfWork = unitOfWork;
        _shippingGateway = shippingGateway;
    }

    public async Task<List<ShippingDistrictDto>> Handle(GetShippingDistrictsQuery request, CancellationToken cancellationToken)
    {
        var districts = await _unitOfWork.ShippingMasterData.GetDistrictsByProvinceIdAsync(request.ProvinceId, cancellationToken);
        if (districts.Count == 0)
        {
            var providerDistricts = await _shippingGateway.GetDistrictsAsync(request.ProvinceId, cancellationToken);
            var normalized = providerDistricts
                .Select(x => ShippingDistrict.Create(x.DistrictId, x.ProvinceId, x.DistrictName, x.SupportType))
                .ToList();

            await _unitOfWork.ShippingMasterData.UpsertDistrictsAsync(normalized, cancellationToken);
            await _unitOfWork.CommitAsync();
            districts = normalized;
        }

        return districts.Select(x => new ShippingDistrictDto
        {
            DistrictId = x.DistrictId,
            ProvinceId = x.ProvinceId,
            DistrictName = x.DistrictName,
            SupportType = x.SupportType
        }).ToList();
    }
}
