using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.Aggregates.ShippingAggregate;

[Table("ShippingDistricts")]
public class ShippingDistrict : EntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int DistrictId { get; private set; }

    public int ProvinceId { get; private set; }

    [Required]
    [MaxLength(200)]
    public string DistrictName { get; private set; } = null!;

    public int? SupportType { get; private set; }

    public bool IsActive { get; private set; } = true;

    private ShippingDistrict()
    {
    }

    public static ShippingDistrict Create(int districtId, int provinceId, string districtName, int? supportType = null)
    {
        if (districtId <= 0)
            throw new ArgumentOutOfRangeException(nameof(districtId));

        if (provinceId <= 0)
            throw new ArgumentOutOfRangeException(nameof(provinceId));

        if (string.IsNullOrWhiteSpace(districtName))
            throw new ArgumentException("District name is required.", nameof(districtName));

        return new ShippingDistrict
        {
            DistrictId = districtId,
            ProvinceId = provinceId,
            DistrictName = districtName.Trim(),
            SupportType = supportType,
            IsActive = true
        };
    }

    public void Update(int provinceId, string districtName, int? supportType, bool isActive = true)
    {
        if (provinceId <= 0)
            throw new ArgumentOutOfRangeException(nameof(provinceId));

        ProvinceId = provinceId;
        DistrictName = string.IsNullOrWhiteSpace(districtName)
            ? throw new ArgumentException("District name is required.", nameof(districtName))
            : districtName.Trim();
        SupportType = supportType;
        IsActive = isActive;
    }
}
