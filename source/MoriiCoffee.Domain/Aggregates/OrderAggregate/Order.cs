using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Domain.Aggregates.OrderAggregate;

/// <summary>
/// Represents a customer order and acts as the aggregate root for all order-related state transitions.
/// </summary>
[Table("Orders")]
public class Order : AggregateRoot
{
    private readonly List<OrderItem> _items = new();

    private Order()
    {
    }

    [Key]
    public Guid Id { get; private set; }

    [Required]
    [MaxLength(20)]
    [Column(TypeName = "nvarchar(20)")]
    public string OrderNumber { get; private set; } = null!;

    [Required]
    public Guid UserId { get; private set; }

    [Required]
    public DeliveryInfo DeliveryInfo { get; private set; } = null!;

    [MaxLength(500)]
    [Column(TypeName = "nvarchar(500)")]
    public string? Notes { get; private set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EPaymentMethod PaymentMethod { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Subtotal { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Tax { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Shipping { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Discount { get; private set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Total { get; private set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EOrderStatus OrderStatus { get; private set; }

    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(
        string orderNumber,
        Guid userId,
        DeliveryInfo deliveryInfo,
        IEnumerable<OrderItem> items,
        EPaymentMethod paymentMethod,
        string? notes = null,
        decimal tax = 0,
        decimal shipping = 0,
        decimal discount = 0) // NOSONAR
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(orderNumber);
        ArgumentNullException.ThrowIfNull(deliveryInfo);
        ArgumentNullException.ThrowIfNull(items);
        ArgumentOutOfRangeException.ThrowIfNegative(tax);
        ArgumentOutOfRangeException.ThrowIfNegative(shipping);
        ArgumentOutOfRangeException.ThrowIfNegative(discount);

        List<OrderItem> orderItems = items.ToList();
        if (orderItems.Count == 0)
        {
            throw new InvalidOperationException("An order must contain at least one item.");
        }

        decimal subtotal = orderItems.Sum(item => item.LineTotal);
        decimal total = subtotal + tax + shipping - discount;
        if (total < 0)
        {
            throw new InvalidOperationException("Order total cannot be negative.");
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = orderNumber.Trim(),
            UserId = userId,
            DeliveryInfo = deliveryInfo,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PaymentMethod = paymentMethod,
            Subtotal = subtotal,
            Tax = tax,
            Shipping = shipping,
            Discount = discount,
            Total = total,
            OrderStatus = EOrderStatus.PENDING
        };

        order._items.AddRange(orderItems.Select(item => item.AssignToOrder(order.Id)));
        return order;
    }

    public void Confirm()
    {
        EnsureStatusIs(EOrderStatus.PENDING, "Only pending orders can be confirmed.");
        OrderStatus = EOrderStatus.CONFIRMED;
    }

    public void MarkReadyToPickup()
    {
        EnsureStatusIs(EOrderStatus.CONFIRMED, "Only confirmed orders can be marked ready to pick up.");
        OrderStatus = EOrderStatus.READY_TO_PICKUP;
    }

    public void MarkInDelivery()
    {
        EnsureStatusIs(EOrderStatus.READY_TO_PICKUP, "Only ready-to-pick-up orders can be marked in delivery.");
        OrderStatus = EOrderStatus.IN_DELIVERY;
    }

    public void MarkDelivered()
    {
        EnsureStatusIs(EOrderStatus.IN_DELIVERY, "Only in-delivery orders can be marked delivered.");
        OrderStatus = EOrderStatus.DELIVERED;
    }

    public void MarkReviewed()
    {
        EnsureStatusIs(EOrderStatus.DELIVERED, "Only delivered orders can be marked reviewed.");
        OrderStatus = EOrderStatus.REVIEWED;
    }

    public void Cancel()
    {
        if (OrderStatus is not EOrderStatus.PENDING and not EOrderStatus.CONFIRMED)
        {
            throw new InvalidOperationException("Only pending or confirmed orders can be cancelled.");
        }

        OrderStatus = EOrderStatus.CANCELLED;
    }

    public void CancelByCustomer()
    {
        EnsureStatusIs(EOrderStatus.PENDING, "Customers can only cancel orders before staff/admin confirmation.");
        OrderStatus = EOrderStatus.CANCELLED;
    }

    public void UpdateStatus(EOrderStatus newStatus)
    {
        if (OrderStatus == newStatus)
        {
            return;
        }

        switch (newStatus)
        {
            case EOrderStatus.CONFIRMED:
                Confirm();
                break;
            case EOrderStatus.READY_TO_PICKUP:
                MarkReadyToPickup();
                break;
            case EOrderStatus.IN_DELIVERY:
                MarkInDelivery();
                break;
            case EOrderStatus.DELIVERED:
                MarkDelivered();
                break;
            case EOrderStatus.REVIEWED:
                MarkReviewed();
                break;
            case EOrderStatus.CANCELLED:
                Cancel();
                break;
            case EOrderStatus.PENDING:
                throw new InvalidOperationException("Order status cannot transition back to pending.");
            default:
                throw new ArgumentOutOfRangeException(nameof(newStatus), newStatus, "Unsupported order status.");
        }
    }

    private void EnsureStatusIs(EOrderStatus expectedStatus, string errorMessage)
    {
        if (OrderStatus != expectedStatus)
        {
            throw new InvalidOperationException(errorMessage);
        }
    }
}
