using FluentAssertions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Domain.Tests.Aggregates;

public class OrderAggregateTests
{
    private static OrderEntity BuildOrder(Guid? userId = null, decimal unitPrice = 45_000, int qty = 1)
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "Cà phê sữa", unitPrice, qty, null, null)
        };
        return OrderEntity.Create("MRC-TEST-001", userId ?? Guid.NewGuid(), delivery, items, EPaymentMethod.COD);
    }

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInputs_StatusIsPending()
    {
        var order = BuildOrder();

        order.OrderStatus.Should().Be(EOrderStatus.PENDING);
    }

    [Fact]
    public void Create_ValidInputs_TotalsAreCalculated()
    {
        var order = BuildOrder(unitPrice: 45_000, qty: 2);

        order.Subtotal.Should().Be(90_000);
        order.Total.Should().Be(90_000);
    }

    [Fact]
    public void Create_EmptyItems_ThrowsInvalidOperationException()
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");

        var act = () => OrderEntity.Create("MRC-TEST-001", Guid.NewGuid(), delivery, [], EPaymentMethod.COD);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*at least one item*");
    }

    [Fact]
    public void Create_NegativeTax_ThrowsArgumentOutOfRangeException()
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem> { OrderItem.Create(Guid.NewGuid(), "Item", 10_000, 1, null, null) };

        var act = () => OrderEntity.Create("MRC-TEST-001", Guid.NewGuid(), delivery, items, EPaymentMethod.COD, tax: -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    // ── Forward transitions — step-by-step ───────────────────────────────────

    [Fact]
    public void Confirm_FromPending_Succeeds()
    {
        var order = BuildOrder();

        order.Confirm();

        order.OrderStatus.Should().Be(EOrderStatus.CONFIRMED);
    }

    [Fact]
    public void MarkReadyToPickup_FromConfirmed_Succeeds()
    {
        var order = BuildOrder();
        order.Confirm();

        order.MarkReadyToPickup();

        order.OrderStatus.Should().Be(EOrderStatus.READY_TO_PICKUP);
    }

    [Fact]
    public void MarkInDelivery_FromReadyToPickup_Succeeds()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();

        order.MarkInDelivery();

        order.OrderStatus.Should().Be(EOrderStatus.IN_DELIVERY);
    }

    [Fact]
    public void MarkDelivered_FromInDelivery_Succeeds()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();
        order.MarkInDelivery();

        order.MarkDelivered();

        order.OrderStatus.Should().Be(EOrderStatus.DELIVERED);
    }

    [Fact]
    public void MarkReviewed_FromDelivered_Succeeds()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();
        order.MarkInDelivery();
        order.MarkDelivered();

        order.MarkReviewed();

        order.OrderStatus.Should().Be(EOrderStatus.REVIEWED);
    }

    // ── Forward transitions — skip steps allowed ──────────────────────────────

    [Theory]
    [InlineData("MarkReadyToPickup")]
    [InlineData("MarkInDelivery")]
    [InlineData("MarkDelivered")]
    [InlineData("MarkReviewed")]
    public void ForwardTransition_FromPending_SkippingSteps_Succeeds(string method)
    {
        var order = BuildOrder();

        var act = () => InvokeTransitionMethod(order, method);

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData("MarkInDelivery")]
    [InlineData("MarkDelivered")]
    [InlineData("MarkReviewed")]
    public void ForwardTransition_FromConfirmed_SkippingSteps_Succeeds(string method)
    {
        var order = BuildOrder();
        order.Confirm();

        var act = () => InvokeTransitionMethod(order, method);

        act.Should().NotThrow();
    }

    // ── Backward transitions blocked ──────────────────────────────────────────

    [Fact]
    public void UpdateStatus_ToPending_ThrowsInvalidOperationException()
    {
        var order = BuildOrder();
        order.Confirm();

        var act = () => order.UpdateStatus(EOrderStatus.PENDING);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*cannot transition back to pending*");
    }

    [Fact]
    public void Confirm_FromConfirmed_ThrowsInvalidOperationException()
    {
        var order = BuildOrder();
        order.Confirm();

        var act = () => order.Confirm();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkReadyToPickup_FromDelivered_ThrowsInvalidOperationException()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();
        order.MarkInDelivery();
        order.MarkDelivered();

        var act = () => order.MarkReadyToPickup();

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MarkInDelivery_FromReviewed_ThrowsInvalidOperationException()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();
        order.MarkInDelivery();
        order.MarkDelivered();
        order.MarkReviewed();

        var act = () => order.MarkInDelivery();

        act.Should().Throw<InvalidOperationException>();
    }

    // ── CANCELLED is terminal ─────────────────────────────────────────────────

    [Fact]
    public void Cancel_FromPending_Succeeds()
    {
        var order = BuildOrder();

        order.Cancel();

        order.OrderStatus.Should().Be(EOrderStatus.CANCELLED);
    }

    [Fact]
    public void Cancel_FromConfirmed_Succeeds()
    {
        var order = BuildOrder();
        order.Confirm();

        order.Cancel();

        order.OrderStatus.Should().Be(EOrderStatus.CANCELLED);
    }

    [Fact]
    public void Cancel_FromReadyToPickup_ThrowsInvalidOperationException()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();

        var act = () => order.Cancel();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*pending or confirmed*");
    }

    [Fact]
    public void Confirm_FromCancelled_ThrowsInvalidOperationException()
    {
        var order = BuildOrder();
        order.Cancel();

        var act = () => order.Confirm();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cancelled orders*");
    }

    [Fact]
    public void MarkDelivered_FromCancelled_ThrowsInvalidOperationException()
    {
        var order = BuildOrder();
        order.Cancel();

        var act = () => order.MarkDelivered();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Cancelled orders*");
    }

    // ── CancelByCustomer ──────────────────────────────────────────────────────

    [Fact]
    public void CancelByCustomer_FromPending_Succeeds()
    {
        var order = BuildOrder();

        order.CancelByCustomer();

        order.OrderStatus.Should().Be(EOrderStatus.CANCELLED);
    }

    [Fact]
    public void CancelByCustomer_FromConfirmed_ThrowsInvalidOperationException()
    {
        var order = BuildOrder();
        order.Confirm();

        var act = () => order.CancelByCustomer();

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*staff/admin confirmation*");
    }

    // ── UpdateStatus routing ──────────────────────────────────────────────────

    [Fact]
    public void UpdateStatus_SameStatus_IsNoOp()
    {
        var order = BuildOrder();

        var act = () => order.UpdateStatus(EOrderStatus.PENDING);

        act.Should().NotThrow();
        order.OrderStatus.Should().Be(EOrderStatus.PENDING);
    }

    [Fact]
    public void UpdateStatus_ToConfirmed_FromPending_Succeeds()
    {
        var order = BuildOrder();

        order.UpdateStatus(EOrderStatus.CONFIRMED);

        order.OrderStatus.Should().Be(EOrderStatus.CONFIRMED);
    }

    [Fact]
    public void UpdateStatus_ToCancelled_FromConfirmed_Succeeds()
    {
        var order = BuildOrder();
        order.Confirm();

        order.UpdateStatus(EOrderStatus.CANCELLED);

        order.OrderStatus.Should().Be(EOrderStatus.CANCELLED);
    }

    // ── GetValidNextStatuses ──────────────────────────────────────────────────

    [Fact]
    public void GetValidNextStatuses_FromPending_IncludesAllForwardAndCancelled()
    {
        var order = BuildOrder();

        var result = order.GetValidNextStatuses();

        result.Should().Contain([
            EOrderStatus.CONFIRMED,
            EOrderStatus.READY_TO_PICKUP,
            EOrderStatus.IN_DELIVERY,
            EOrderStatus.DELIVERED,
            EOrderStatus.REVIEWED,
            EOrderStatus.CANCELLED
        ]);
    }

    [Fact]
    public void GetValidNextStatuses_FromConfirmed_IncludesCancelled()
    {
        var order = BuildOrder();
        order.Confirm();

        var result = order.GetValidNextStatuses();

        result.Should().Contain(EOrderStatus.CANCELLED);
        result.Should().NotContain(EOrderStatus.PENDING);
        result.Should().NotContain(EOrderStatus.CONFIRMED);
    }

    [Fact]
    public void GetValidNextStatuses_FromReadyToPickup_ExcludesCancelled()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();

        var result = order.GetValidNextStatuses();

        result.Should().NotContain(EOrderStatus.CANCELLED);
        result.Should().Contain([EOrderStatus.IN_DELIVERY, EOrderStatus.DELIVERED, EOrderStatus.REVIEWED]);
    }

    [Fact]
    public void GetValidNextStatuses_FromDelivered_OnlyReviewed()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();
        order.MarkInDelivery();
        order.MarkDelivered();

        var result = order.GetValidNextStatuses();

        result.Should().ContainSingle().Which.Should().Be(EOrderStatus.REVIEWED);
    }

    [Fact]
    public void GetValidNextStatuses_FromReviewed_IsEmpty()
    {
        var order = BuildOrder();
        order.Confirm();
        order.MarkReadyToPickup();
        order.MarkInDelivery();
        order.MarkDelivered();
        order.MarkReviewed();

        var result = order.GetValidNextStatuses();

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetValidNextStatuses_FromCancelled_IsEmpty()
    {
        var order = BuildOrder();
        order.Cancel();

        var result = order.GetValidNextStatuses();

        result.Should().BeEmpty();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static void InvokeTransitionMethod(OrderEntity order, string method)
    {
        Action action = method switch
        {
            "Confirm" => order.Confirm,
            "MarkReadyToPickup" => order.MarkReadyToPickup,
            "MarkInDelivery" => order.MarkInDelivery,
            "MarkDelivered" => order.MarkDelivered,
            "MarkReviewed" => order.MarkReviewed,
            _ => throw new ArgumentOutOfRangeException(nameof(method))
        };
        action();
    }
}
