using MoriiCoffee.Domain.Aggregates.ShippingAggregate;

namespace MoriiCoffee.Domain.Repositories;

public interface IShippingMasterDataRepository
{
    Task<List<ShippingProvince>> GetProvincesAsync(CancellationToken cancellationToken = default);

    Task<List<ShippingDistrict>> GetDistrictsByProvinceIdAsync(int provinceId, CancellationToken cancellationToken = default);

    Task<List<ShippingWard>> GetWardsByDistrictIdAsync(int districtId, CancellationToken cancellationToken = default);

    Task UpsertProvincesAsync(IEnumerable<ShippingProvince> provinces, CancellationToken cancellationToken = default);

    Task UpsertDistrictsAsync(IEnumerable<ShippingDistrict> districts, CancellationToken cancellationToken = default);

    Task UpsertWardsAsync(IEnumerable<ShippingWard> wards, CancellationToken cancellationToken = default);
}
