using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;

/// <summary>
/// Immutable delivery snapshot stored directly with an order.
/// </summary>
public record DeliveryInfo
{
    public DeliveryInfo(string fullName, string phoneNumber, string address)
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
}
