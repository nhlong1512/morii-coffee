using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;

/// <summary>
/// Stores the user's default delivery information for checkout convenience.
/// </summary>
[Table("UserDeliveryProfiles")]
public class UserDeliveryProfile : EntityBase
{
    [Key]
    [Required]
    public Guid UserId { get; private set; }

    [Required]
    [MaxLength(100)]
    public string FullName { get; private set; } = null!;

    [Required]
    [MaxLength(15)]
    public string PhoneNumber { get; private set; } = null!;

    [Required]
    [MaxLength(500)]
    public string Address { get; private set; } = null!;

    public int? ProvinceId { get; private set; }

    [MaxLength(200)]
    public string? ProvinceName { get; private set; }

    public int? DistrictId { get; private set; }

    [MaxLength(200)]
    public string? DistrictName { get; private set; }

    [MaxLength(50)]
    public string? WardCode { get; private set; }

    [MaxLength(200)]
    public string? WardName { get; private set; }

    private UserDeliveryProfile()
    {
    }

    public static UserDeliveryProfile Create(
        Guid userId,
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
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId must not be empty.", nameof(userId));
        }

        return new UserDeliveryProfile
        {
            UserId = userId,
            FullName = string.IsNullOrWhiteSpace(fullName)
                ? throw new ArgumentException("Full name is required.", nameof(fullName))
                : fullName.Trim(),
            PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
                ? throw new ArgumentException("Phone number is required.", nameof(phoneNumber))
                : phoneNumber.Trim(),
            Address = string.IsNullOrWhiteSpace(address)
                ? throw new ArgumentException("Address is required.", nameof(address))
                : address.Trim(),
            ProvinceId = provinceId,
            ProvinceName = string.IsNullOrWhiteSpace(provinceName) ? null : provinceName.Trim(),
            DistrictId = districtId,
            DistrictName = string.IsNullOrWhiteSpace(districtName) ? null : districtName.Trim(),
            WardCode = string.IsNullOrWhiteSpace(wardCode) ? null : wardCode.Trim(),
            WardName = string.IsNullOrWhiteSpace(wardName) ? null : wardName.Trim()
        };
    }

    public void Update(
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
            ? throw new ArgumentException("Full name is required.", nameof(fullName))
            : fullName.Trim();
        PhoneNumber = string.IsNullOrWhiteSpace(phoneNumber)
            ? throw new ArgumentException("Phone number is required.", nameof(phoneNumber))
            : phoneNumber.Trim();
        Address = string.IsNullOrWhiteSpace(address)
            ? throw new ArgumentException("Address is required.", nameof(address))
            : address.Trim();
        ProvinceId = provinceId;
        ProvinceName = string.IsNullOrWhiteSpace(provinceName) ? null : provinceName.Trim();
        DistrictId = districtId;
        DistrictName = string.IsNullOrWhiteSpace(districtName) ? null : districtName.Trim();
        WardCode = string.IsNullOrWhiteSpace(wardCode) ? null : wardCode.Trim();
        WardName = string.IsNullOrWhiteSpace(wardName) ? null : wardName.Trim();
    }
}
