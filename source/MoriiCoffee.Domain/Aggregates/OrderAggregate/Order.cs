using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.SeedWork.AggregateRoot;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;

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
    public string OrderNumber { get; private set; } = null!;

    [Required]
    public Guid UserId { get; private set; }

    [Required]
    public DeliveryInfo DeliveryInfo { get; private set; } = null!;

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EDeliveryMethod DeliveryMethod { get; private set; } = EDeliveryMethod.GHN_DELIVERY;

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EShippingProvider? ShippingProvider { get; private set; }

    [MaxLength(100)]
    public string? ShippingQuoteFingerprint { get; private set; }

    public int? ShippingServiceId { get; private set; }

    public int? ShippingServiceTypeId { get; private set; }

    [MaxLength(200)]
    public string? ShippingServiceLabel { get; private set; }

    [MaxLength(50)]
    public string? ShippingProviderEnvironment { get; private set; }

    public DateTime? ShippingQuoteExpiresAt { get; private set; }

    [MaxLength(500)]
    public string? Notes { get; private set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EPaymentMethod PaymentMethod { get; private set; }

    public decimal Subtotal { get; private set; }

    public decimal Tax { get; private set; }

    public decimal Shipping { get; private set; }

    public decimal Discount { get; private set; }

    public decimal Total { get; private set; }

    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EOrderStatus OrderStatus { get; private set; }

    /// <summary>
    /// Payment lifecycle, kept distinct from <see cref="OrderStatus"/> (which is fulfilment).
    /// For COD orders this is <see cref="EPaymentStatus.NotRequired"/>.
    /// For Stripe orders this starts at <see cref="EPaymentStatus.Pending"/> and is driven by
    /// webhook events.
    /// </summary>
    [Required]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public EPaymentStatus PaymentStatus { get; private set; }

    /// <summary>
    /// Stripe PaymentIntent id of the successful payment (e.g. <c>pi_3OZA...</c>). Null for COD
    /// orders and for orders still in <see cref="EPaymentStatus.Pending"/> / <see cref="EPaymentStatus.Failed"/>.
    /// </summary>
    [MaxLength(200)]
    public string? StripePaymentIntentId { get; private set; }

    /// <summary>
    /// Stripe Charge id of the successful payment (<c>ch_...</c>). Null until payment succeeds.
    /// </summary>
    [MaxLength(200)]
    public string? StripeChargeId { get; private set; }

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
        decimal discount = 0,
        EDeliveryMethod deliveryMethod = EDeliveryMethod.GHN_DELIVERY,
        EShippingProvider? shippingProvider = null,
        string? shippingQuoteFingerprint = null,
        int? shippingServiceId = null,
        int? shippingServiceTypeId = null,
        string? shippingServiceLabel = null,
        string? shippingProviderEnvironment = null,
        DateTime? shippingQuoteExpiresAt = null) // NOSONAR
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
            DeliveryMethod = deliveryMethod,
            ShippingProvider = shippingProvider,
            ShippingQuoteFingerprint = string.IsNullOrWhiteSpace(shippingQuoteFingerprint) ? null : shippingQuoteFingerprint.Trim(),
            ShippingServiceId = shippingServiceId,
            ShippingServiceTypeId = shippingServiceTypeId,
            ShippingServiceLabel = string.IsNullOrWhiteSpace(shippingServiceLabel) ? null : shippingServiceLabel.Trim(),
            ShippingProviderEnvironment = string.IsNullOrWhiteSpace(shippingProviderEnvironment) ? null : shippingProviderEnvironment.Trim(),
            ShippingQuoteExpiresAt = shippingQuoteExpiresAt,
            Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim(),
            PaymentMethod = paymentMethod,
            Subtotal = subtotal,
            Tax = tax,
            Shipping = shipping,
            Discount = discount,
            Total = total,
            OrderStatus = EOrderStatus.PENDING,
            // Initial PaymentStatus: COD = NotRequired (legacy zero-touch behaviour);
            // STRIPE / future online providers = Pending (awaiting webhook confirmation).
            PaymentStatus = paymentMethod == EPaymentMethod.COD
                ? EPaymentStatus.NotRequired
                : EPaymentStatus.Pending
        };

        order._items.AddRange(orderItems.Select(item => item.AssignToOrder(order.Id)));
        return order;
    }

    public void ApplyShippingQuote(
        EShippingProvider shippingProvider,
        string shippingQuoteFingerprint,
        int serviceId,
        int? serviceTypeId,
        string? serviceLabel,
        string providerEnvironment,
        DateTime quoteExpiresAtUtc,
        decimal shippingFee)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(shippingQuoteFingerprint);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerEnvironment);
        ArgumentOutOfRangeException.ThrowIfNegative(shippingFee);

        ShippingProvider = shippingProvider;
        ShippingQuoteFingerprint = shippingQuoteFingerprint.Trim();
        ShippingServiceId = serviceId;
        ShippingServiceTypeId = serviceTypeId;
        ShippingServiceLabel = string.IsNullOrWhiteSpace(serviceLabel) ? null : serviceLabel.Trim();
        ShippingProviderEnvironment = providerEnvironment.Trim();
        ShippingQuoteExpiresAt = quoteExpiresAtUtc;
        Shipping = shippingFee;
        Total = Subtotal + Tax + Shipping - Discount;
    }

    /// <summary>
    /// Flips <see cref="PaymentStatus"/> to <see cref="EPaymentStatus.Paid"/> after the
    /// <c>checkout.session.completed</c> webhook is verified. Idempotent: re-applying the same
    /// PaymentIntent id is a no-op; applying a different one throws (defensive — a same-order
    /// successful charge with a different PI id would be a data-integrity bug).
    /// </summary>
    public void MarkPaymentPaid(string stripePaymentIntentId, string stripeChargeId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(stripePaymentIntentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(stripeChargeId);

        if (PaymentStatus == EPaymentStatus.Paid)
        {
            if (!string.Equals(StripePaymentIntentId, stripePaymentIntentId, StringComparison.Ordinal))
                throw new InvalidOperationException(
                    "Order is already Paid with a different PaymentIntent id.");
            return; // idempotent no-op
        }

        if (PaymentStatus != EPaymentStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot transition payment from {PaymentStatus} to Paid.");

        PaymentStatus = EPaymentStatus.Paid;
        StripePaymentIntentId = stripePaymentIntentId.Trim();
        StripeChargeId = stripeChargeId.Trim();

    }

    /// <summary>
    /// Flips <see cref="PaymentStatus"/> to <see cref="EPaymentStatus.Failed"/> after Stripe
    /// signals a definitive failure (<c>checkout.session.expired</c> or
    /// <c>payment_intent.payment_failed</c>). Idempotent.
    /// </summary>
    public void MarkPaymentFailed()
    {
        if (PaymentStatus == EPaymentStatus.Failed)
            return;

        if (PaymentStatus != EPaymentStatus.Pending)
            throw new InvalidOperationException(
                $"Cannot transition payment from {PaymentStatus} to Failed.");

        PaymentStatus = EPaymentStatus.Failed;
    }

    /// <summary>
    /// Updates the order's <see cref="PaymentStatus"/> based on the running cumulative refund
    /// total recorded against the underlying <see cref="PaymentAggregate.Payment"/>.
    /// </summary>
    /// <param name="cumulativeRefundedAmount">Sum of all settled refunds for the order's payment.</param>
    public void ApplyRefund(decimal cumulativeRefundedAmount)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cumulativeRefundedAmount);

        if (PaymentStatus is not (EPaymentStatus.Paid or EPaymentStatus.PartiallyRefunded))
            throw new InvalidOperationException(
                $"Cannot apply a refund while payment status is {PaymentStatus}.");

        if (cumulativeRefundedAmount > Total)
            throw new InvalidOperationException(
                "Cumulative refunded amount cannot exceed the order total.");

        PaymentStatus = cumulativeRefundedAmount == Total
            ? EPaymentStatus.Refunded
            : EPaymentStatus.PartiallyRefunded;
    }

    public void Confirm()
    {
        // FR-013: an online-paid order MUST NOT be confirmed for fulfilment until payment is
        // settled. COD orders bypass this guard (PaymentStatus == NotRequired).
        if (PaymentMethod != EPaymentMethod.COD &&
            PaymentStatus is EPaymentStatus.Pending or EPaymentStatus.Failed)
        {
            throw new InvalidOperationException(
                $"Cannot confirm an order whose payment status is {PaymentStatus}.");
        }

        EnsureCanAdvanceTo(EOrderStatus.CONFIRMED);

        OrderStatus = EOrderStatus.CONFIRMED;
    }

    public void MarkReadyToPickup()
    {
        EnsureCanAdvanceTo(EOrderStatus.READY_TO_PICKUP);
        OrderStatus = EOrderStatus.READY_TO_PICKUP;
    }

    public void MarkInDelivery()
    {
        EnsureCanAdvanceTo(EOrderStatus.IN_DELIVERY);
        OrderStatus = EOrderStatus.IN_DELIVERY;
    }

    public void MarkDelivered()
    {
        EnsureCanAdvanceTo(EOrderStatus.DELIVERED);
        OrderStatus = EOrderStatus.DELIVERED;
    }

    public void MarkReviewed()
    {
        EnsureCanAdvanceTo(EOrderStatus.REVIEWED);
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
        if (OrderStatus != EOrderStatus.PENDING)
        {
            throw new InvalidOperationException("Customers can only cancel orders before staff/admin confirmation.");
        }

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

    private static readonly Dictionary<EOrderStatus, int> StatusRank = new()
    {
        { EOrderStatus.PENDING, 0 },
        { EOrderStatus.CONFIRMED, 1 },
        { EOrderStatus.READY_TO_PICKUP, 2 },
        { EOrderStatus.IN_DELIVERY, 3 },
        { EOrderStatus.DELIVERED, 4 },
        { EOrderStatus.REVIEWED, 5 }
    };

    public IReadOnlyList<EOrderStatus> GetValidNextStatuses()
    {
        // CANCELLED is not in StatusRank, REVIEWED has rank 5 (no forward statuses exist)
        if (!StatusRank.TryGetValue(OrderStatus, out var currentRank))
            return [];

        var next = StatusRank
            .Where(kv => kv.Value > currentRank)
            .Select(kv => kv.Key)
            .ToList();

        // CANCELLED is only reachable from PENDING or CONFIRMED (admin Cancel rule)
        if (OrderStatus is EOrderStatus.PENDING or EOrderStatus.CONFIRMED)
            next.Add(EOrderStatus.CANCELLED);

        return next.AsReadOnly();
    }

    private void EnsureCanAdvanceTo(EOrderStatus target)
    {
        if (OrderStatus == EOrderStatus.CANCELLED)
            throw new InvalidOperationException("Cancelled orders cannot be updated.");

        if (!StatusRank.TryGetValue(OrderStatus, out var current) || !StatusRank.TryGetValue(target, out var next))
            throw new InvalidOperationException($"Unsupported status transition from {OrderStatus} to {target}.");

        if (current >= next)
            throw new InvalidOperationException($"Cannot transition from {OrderStatus} to {target}.");
    }
}
