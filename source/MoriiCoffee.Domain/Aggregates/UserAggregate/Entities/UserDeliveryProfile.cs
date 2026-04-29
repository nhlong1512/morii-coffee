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
    [Column(TypeName = "nvarchar(100)")]
    public string FullName { get; private set; } = null!;

    [Required]
    [MaxLength(15)]
    [Column(TypeName = "nvarchar(15)")]
    public string PhoneNumber { get; private set; } = null!;

    [Required]
    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string Address { get; private set; } = null!;

    private UserDeliveryProfile()
    {
    }

    public static UserDeliveryProfile Create(Guid userId, string fullName, string phoneNumber, string address)
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
                : address.Trim()
        };
    }

    public void Update(string fullName, string phoneNumber, string address)
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
    }
}
