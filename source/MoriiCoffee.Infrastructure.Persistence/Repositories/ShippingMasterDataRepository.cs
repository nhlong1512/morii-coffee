using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.ShippingAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Infrastructure.Persistence.Data;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

public class ShippingMasterDataRepository : IShippingMasterDataRepository
{
    private readonly ApplicationDbContext _context;

    public ShippingMasterDataRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ShippingProvince>> GetProvincesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.ShippingProvinces
            .Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.ProvinceName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ShippingDistrict>> GetDistrictsByProvinceIdAsync(int provinceId, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingDistricts
            .Where(x => !x.IsDeleted && x.IsActive && x.ProvinceId == provinceId)
            .OrderBy(x => x.DistrictName)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<ShippingWard>> GetWardsByDistrictIdAsync(int districtId, CancellationToken cancellationToken = default)
    {
        return await _context.ShippingWards
            .Where(x => !x.IsDeleted && x.IsActive && x.DistrictId == districtId)
            .OrderBy(x => x.WardName)
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertProvincesAsync(IEnumerable<ShippingProvince> provinces, CancellationToken cancellationToken = default)
    {
        foreach (var province in provinces)
        {
            var existing = await _context.ShippingProvinces
                .FirstOrDefaultAsync(x => x.ProvinceId == province.ProvinceId, cancellationToken);

            if (existing is null)
            {
                await _context.ShippingProvinces.AddAsync(province, cancellationToken);
                continue;
            }

            existing.Update(province.ProvinceName, province.Code, province.IsActive);
        }
    }

    public async Task UpsertDistrictsAsync(IEnumerable<ShippingDistrict> districts, CancellationToken cancellationToken = default)
    {
        foreach (var district in districts)
        {
            var existing = await _context.ShippingDistricts
                .FirstOrDefaultAsync(x => x.DistrictId == district.DistrictId, cancellationToken);

            if (existing is null)
            {
                await _context.ShippingDistricts.AddAsync(district, cancellationToken);
                continue;
            }

            existing.Update(district.ProvinceId, district.DistrictName, district.SupportType, district.IsActive);
        }
    }

    public async Task UpsertWardsAsync(IEnumerable<ShippingWard> wards, CancellationToken cancellationToken = default)
    {
        foreach (var ward in wards)
        {
            var existing = await _context.ShippingWards
                .FirstOrDefaultAsync(x => x.WardCode == ward.WardCode, cancellationToken);

            if (existing is null)
            {
                await _context.ShippingWards.AddAsync(ward, cancellationToken);
                continue;
            }

            existing.Update(ward.DistrictId, ward.WardName, ward.IsActive);
        }
    }
}
