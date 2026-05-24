using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.Aggregates.ShippingAggregate;

[Table("ShippingWards")]
public class ShippingWard : EntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    [MaxLength(50)]
    public string WardCode { get; private set; } = null!;

    public int DistrictId { get; private set; }

    [Required]
    [MaxLength(200)]
    public string WardName { get; private set; } = null!;

    public bool IsActive { get; private set; } = true;

    private ShippingWard()
    {
    }

    public static ShippingWard Create(string wardCode, int districtId, string wardName)
    {
        if (string.IsNullOrWhiteSpace(wardCode))
            throw new ArgumentException("Ward code is required.", nameof(wardCode));

        if (districtId <= 0)
            throw new ArgumentOutOfRangeException(nameof(districtId));

        if (string.IsNullOrWhiteSpace(wardName))
            throw new ArgumentException("Ward name is required.", nameof(wardName));

        return new ShippingWard
        {
            WardCode = wardCode.Trim(),
            DistrictId = districtId,
            WardName = wardName.Trim(),
            IsActive = true
        };
    }

    public void Update(int districtId, string wardName, bool isActive = true)
    {
        if (districtId <= 0)
            throw new ArgumentOutOfRangeException(nameof(districtId));

        DistrictId = districtId;
        WardName = string.IsNullOrWhiteSpace(wardName)
            ? throw new ArgumentException("Ward name is required.", nameof(wardName))
            : wardName.Trim();
        IsActive = isActive;
    }
}
