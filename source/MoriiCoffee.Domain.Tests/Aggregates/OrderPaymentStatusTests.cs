using FluentAssertions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Domain.Tests.Aggregates;

/// <summary>
/// Domain-level tests for the payment-status state machine on <see cref="OrderEntity"/>.
/// Covers feature 011-stripe-payment FRs and acceptance scenarios at the aggregate level.
/// </summary>
public class OrderPaymentStatusTests
{
    [Fact]
    public void Create_CodOrder_StartsWithNotRequired()
    {
        var order = Build(EPaymentMethod.COD);
        order.PaymentStatus.Should().Be(EPaymentStatus.NotRequired);
    }

    [Fact]
    public void Create_StripeOrder_StartsWithPending()
    {
        var order = Build(EPaymentMethod.STRIPE);
        order.PaymentStatus.Should().Be(EPaymentStatus.Pending);
        order.OrderStatus.Should().Be(EOrderStatus.PENDING);
    }

    [Fact]
    public void MarkPaymentPaid_FromPending_TransitionsToPaid()
    {
        var order = Build(EPaymentMethod.STRIPE);

        order.MarkPaymentPaid("pi_abc", "ch_abc");

        order.PaymentStatus.Should().Be(EPaymentStatus.Paid);
        order.StripePaymentIntentId.Should().Be("pi_abc");
        order.StripeChargeId.Should().Be("ch_abc");
        order.OrderStatus.Should().Be(EOrderStatus.PENDING);
    }

    [Fact]
    public void MarkPaymentPaid_SamePaymentIntentId_IsIdempotent()
    {
        var order = Build(EPaymentMethod.STRIPE);
        order.MarkPaymentPaid("pi_abc", "ch_abc");

        // Second call must be a no-op (no throw, no state change).
        var act = () => order.MarkPaymentPaid("pi_abc", "ch_abc");

        act.Should().NotThrow();
        order.PaymentStatus.Should().Be(EPaymentStatus.Paid);
    }

    [Fact]
    public void MarkPaymentPaid_DifferentPaymentIntentId_Throws()
    {
        var order = Build(EPaymentMethod.STRIPE);
        order.MarkPaymentPaid("pi_abc", "ch_abc");

        var act = () => order.MarkPaymentPaid("pi_different", "ch_different");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*different PaymentIntent*");
    }

    [Fact]
    public void MarkPaymentFailed_FromPending_TransitionsToFailed()
    {
        var order = Build(EPaymentMethod.STRIPE);
        order.MarkPaymentFailed();
        order.PaymentStatus.Should().Be(EPaymentStatus.Failed);
    }

    [Fact]
    public void MarkPaymentFailed_FromPaid_Throws()
    {
        var order = Build(EPaymentMethod.STRIPE);
        order.MarkPaymentPaid("pi_abc", "ch_abc");

        var act = () => order.MarkPaymentFailed();
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void ApplyRefund_FullAmount_FlipsToRefunded()
    {
        var order = Build(EPaymentMethod.STRIPE, unitPrice: 100_000m);
        order.MarkPaymentPaid("pi_abc", "ch_abc");

        order.ApplyRefund(order.Total);
        order.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
    }

    [Fact]
    public void ApplyRefund_PartialAmount_FlipsToPartiallyRefunded()
    {
        var order = Build(EPaymentMethod.STRIPE, unitPrice: 100_000m);
        order.MarkPaymentPaid("pi_abc", "ch_abc");

        order.ApplyRefund(40_000m);
        order.PaymentStatus.Should().Be(EPaymentStatus.PartiallyRefunded);
    }

    [Fact]
    public void ApplyRefund_RefundedToFullyRefunded_Transitions()
    {
        var order = Build(EPaymentMethod.STRIPE, unitPrice: 100_000m);
        order.MarkPaymentPaid("pi_abc", "ch_abc");
        order.ApplyRefund(40_000m); // PartiallyRefunded
        order.ApplyRefund(order.Total); // cumulative == total
        order.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
    }

    [Fact]
    public void Confirm_StripeOrderInPendingPayment_Throws()
    {
        var order = Build(EPaymentMethod.STRIPE);

        var act = () => order.Confirm();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*payment status*");
    }

    [Fact]
    public void Confirm_CodOrder_Succeeds()
    {
        var order = Build(EPaymentMethod.COD);
        var act = () => order.Confirm();
        act.Should().NotThrow();
        order.OrderStatus.Should().Be(EOrderStatus.CONFIRMED);
    }

    [Fact]
    public void Confirm_StripeOrderPaid_Succeeds()
    {
        var order = Build(EPaymentMethod.STRIPE);
        order.MarkPaymentPaid("pi_abc", "ch_abc");

        var act = () => order.Confirm();
        act.Should().NotThrow();
        order.OrderStatus.Should().Be(EOrderStatus.CONFIRMED);
    }

    // ── Helpers ────────────────────────────────────────────────────────────

    private static OrderEntity Build(EPaymentMethod method, decimal unitPrice = 45_000m, int qty = 1)
    {
        return OrderEntity.Create(
            "MRC-TEST-PAY-001",
            Guid.NewGuid(),
            new DeliveryInfo("Test", "0900000000", "1 Test St"),
            new[] { OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", unitPrice, qty) },
            method);
    }
}
