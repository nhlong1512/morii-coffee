using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;

/// <summary>
/// Immutable delivery snapshot stored directly with an order.
/// </summary>
public record DeliveryInfo
{
    public DeliveryInfo(
        string fullName,
        string phoneNumber,
        string address,
        int? provinceId = null,
        string? provinceName = null,
        int? districtId = null,
        string? districtName = null,
        string? wardCode = null,
        string? wardName = null)
    {
        FullName = string.IsNullOrWhiteSpace(fullName)
            ? throw new ArgumentException("Delivery full name is required.", nameof(fullName))
            : fullName.Trim();

        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
            ? throw new ArgumentException("Delivery phone number is required.", nameof(phoneNumber))
            : phoneNumber.Trim();

        Address = string.IsNullOrWhiteSpace(address)
            ? throw new ArgumentException("Delivery address is required.", nameof(address))
            : address.Trim();

        ProvinceId = provinceId;
        ProvinceName = string.IsNullOrWhiteSpace(provinceName) ? null : provinceName.Trim();
        DistrictId = districtId;
        DistrictName = string.IsNullOrWhiteSpace(districtName) ? null : districtName.Trim();
        WardCode = string.IsNullOrWhiteSpace(wardCode) ? null : wardCode.Trim();
        WardName = string.IsNullOrWhiteSpace(wardName) ? null : wardName.Trim();
    }

    [Required]
    [MaxLength(100)]
    public string FullName { get; init; }

    [Required]
    [MaxLength(15)]
    public string PhoneNumber { get; init; }

    [Required]
    [MaxLength(500)]
    public string Address { get; init; }

    public int? ProvinceId { get; init; }

    [MaxLength(200)]
    public string? ProvinceName { get; init; }

    public int? DistrictId { get; init; }

    [MaxLength(200)]
    public string? DistrictName { get; init; }

    [MaxLength(50)]
    public string? WardCode { get; init; }

    [MaxLength(200)]
    public string? WardName { get; init; }
}
