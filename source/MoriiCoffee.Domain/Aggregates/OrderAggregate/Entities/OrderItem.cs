using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using MoriiCoffee.Domain.SeedWork.Entities;

namespace MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;

/// <summary>
/// Snapshot of a purchased product inside an order.
/// </summary>
[Table("OrderItems")]
public class OrderItem : EntityBase
{
    private OrderItem()
    {
    }

    [Key]
    public Guid Id { get; private set; }

    [Required]
    public Guid OrderId { get; private set; }

    [Required]
    public Guid ProductId { get; private set; }

    [Required]
    [MaxLength(200)]
    [Column(TypeName = "nvarchar(200)")]
    public string ProductName { get; private set; } = null!;

    public Guid? VariantId { get; private set; }

    [MaxLength(100)]
    [Column(TypeName = "nvarchar(100)")]
    public string? VariantLabel { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal UnitPrice { get; private set; }

    public int Quantity { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal LineTotal { get; private set; }

    public static OrderItem Create(
        Guid productId,
        string productName,
        decimal unitPrice,
        int quantity,
        Guid? variantId = null,
        string? variantLabel = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(productName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(quantity);
        ArgumentOutOfRangeException.ThrowIfNegative(unitPrice);

        return new OrderItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName.Trim(),
            VariantId = variantId,
            VariantLabel = string.IsNullOrWhiteSpace(variantLabel) ? null : variantLabel.Trim(),
            UnitPrice = unitPrice,
            Quantity = quantity,
            LineTotal = unitPrice * quantity
        };
    }

    internal OrderItem AssignToOrder(Guid orderId)
    {
        OrderId = orderId;
        return this;
    }
}
