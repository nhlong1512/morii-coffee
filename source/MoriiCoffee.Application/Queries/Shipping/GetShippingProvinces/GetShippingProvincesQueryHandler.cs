using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Shipping.GetShippingProvinces;

public class GetShippingProvincesQueryHandler : IQueryHandler<GetShippingProvincesQuery, List<ShippingProvinceDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IShippingGateway _shippingGateway;

    public GetShippingProvincesQueryHandler(IUnitOfWork unitOfWork, IShippingGateway shippingGateway)
    {
        _unitOfWork = unitOfWork;
        _shippingGateway = shippingGateway;
    }

    public async Task<List<ShippingProvinceDto>> Handle(GetShippingProvincesQuery request, CancellationToken cancellationToken)
    {
        var provinces = await _unitOfWork.ShippingMasterData.GetProvincesAsync(cancellationToken);
        if (provinces.Count == 0)
        {
            var providerProvinces = await _shippingGateway.GetProvincesAsync(cancellationToken);
            var normalized = providerProvinces
                .Select(x => ShippingProvince.Create(x.ProvinceId, x.ProvinceName, x.Code))
                .ToList();

            await _unitOfWork.ShippingMasterData.UpsertProvincesAsync(normalized, cancellationToken);
            await _unitOfWork.CommitAsync();
            provinces = normalized;
        }

        return provinces.Select(x => new ShippingProvinceDto
        {
            ProvinceId = x.ProvinceId,
            ProvinceName = x.ProvinceName,
            Code = x.Code
        }).ToList();
    }
}
