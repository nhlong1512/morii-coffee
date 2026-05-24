using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.Aggregates.ShippingAggregate;

[Table("ShippingProvinces")]
public class ShippingProvince : EntityBase
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int ProvinceId { get; private set; }

    [Required]
    [MaxLength(200)]
    public string ProvinceName { get; private set; } = null!;

    [MaxLength(50)]
    public string? Code { get; private set; }

    public bool IsActive { get; private set; } = true;

    private ShippingProvince()
    {
    }

    public static ShippingProvince Create(int provinceId, string provinceName, string? code = null)
    {
        if (provinceId <= 0)
            throw new ArgumentOutOfRangeException(nameof(provinceId));

        if (string.IsNullOrWhiteSpace(provinceName))
            throw new ArgumentException("Province name is required.", nameof(provinceName));

        return new ShippingProvince
        {
            ProvinceId = provinceId,
            ProvinceName = provinceName.Trim(),
            Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim(),
            IsActive = true
        };
    }

    public void Update(string provinceName, string? code, bool isActive = true)
    {
        ProvinceName = string.IsNullOrWhiteSpace(provinceName)
            ? throw new ArgumentException("Province name is required.", nameof(provinceName))
            : provinceName.Trim();
        Code = string.IsNullOrWhiteSpace(code) ? null : code.Trim();
        IsActive = isActive;
    }
}
