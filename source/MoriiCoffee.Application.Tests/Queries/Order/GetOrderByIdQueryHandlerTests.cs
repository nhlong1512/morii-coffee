using FluentAssertions;
using MockQueryable;
using Moq;
using MoriiCoffee.Application.Queries.Order.GetOrderById;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Tests.Queries.Order;

public class GetOrderByIdQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly GetOrderByIdQueryHandler _handler;
    private static readonly AwsS3Settings S3Settings = new() { CdnBaseUrl = "https://cdn.test" };

    public GetOrderByIdQueryHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _productsRepo
            .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Aggregates.ProductAggregate.Product, bool>>>(), false))
            .Returns(new List<Domain.Aggregates.ProductAggregate.Product>().BuildMock());
        _paymentsRepo
            .Setup(r => r.ListByOrderIdAsync(It.IsAny<Guid>()))
            .ReturnsAsync([]);
        _handler = new GetOrderByIdQueryHandler(_unitOfWork.Object, S3Settings);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ThrowsNotFoundException()
    {
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrderEntity?)null);

        var act = () => _handler.Handle(
            new GetOrderByIdQuery(Guid.NewGuid(), Guid.NewGuid(), false),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_NonOwner_NonAdmin_ThrowsUnauthorizedException()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var act = () => _handler.Handle(
            new GetOrderByIdQuery(order.Id, Guid.NewGuid(), false),
            CancellationToken.None);

        await act.Should().ThrowAsync<UnauthorizedException>();
    }

    [Fact]
    public async Task Handle_Owner_ReturnsOrderDto()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(
            new GetOrderByIdQuery(order.Id, ownerId, false),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
        result.UserId.Should().Be(ownerId);
        result.PaymentInfo.PaymentStatus.Should().Be(EPaymentStatus.NotRequired);
    }

    [Fact]
    public async Task Handle_AdminCanViewAnyOrder()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);

        var result = await _handler.Handle(
            new GetOrderByIdQuery(order.Id, Guid.NewGuid(), IsAdmin: true),
            CancellationToken.None);

        result.Should().NotBeNull();
        result.Id.Should().Be(order.Id);
    }

    [Fact]
    public async Task Handle_ProductThumbnailStorageKey_ResolvesToCdnUrl()
    {
        var ownerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var order = BuildOrder(ownerId, productId);
        var products = new List<Domain.Aggregates.ProductAggregate.Product>
        {
            new()
            {
                Id = productId,
                ThumbnailUrl = "products/abc/123-photo.png"
            }
        };

        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        _productsRepo
            .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<Domain.Aggregates.ProductAggregate.Product, bool>>>(), false))
            .Returns(products.BuildMock());

        var result = await _handler.Handle(
            new GetOrderByIdQuery(order.Id, ownerId, false),
            CancellationToken.None);

        result.Items.Single().ImageUrl.Should().Be("https://cdn.test/products/abc/123-photo.png");
    }

    [Fact]
    public async Task Handle_OrderHasPaymentAttempts_ReturnsEmbeddedPaymentInfo()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildOrder(ownerId);
        var payment1 = PaymentEntity.Create(order.Id, "cs_old", 137000m, "vnd");
        payment1.MarkFailed("Card declined");
        var payment2 = PaymentEntity.Create(order.Id, "cs_new", 137000m, "vnd");
        payment2.MarkSucceeded("pi_paid", "ch_paid");

        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo
            .Setup(r => r.ListByOrderIdAsync(order.Id))
            .ReturnsAsync([payment2, payment1]);

        var result = await _handler.Handle(
            new GetOrderByIdQuery(order.Id, ownerId, false),
            CancellationToken.None);

        result.PaymentInfo.PaymentStatus.Should().Be(EPaymentStatus.NotRequired);
        result.PaymentInfo.AttemptCount.Should().Be(2);
        result.PaymentInfo.LatestPaymentId.Should().Be(payment2.Id);
        result.PaymentInfo.LatestAttemptStatus.Should().Be(EPaymentTransactionStatus.Succeeded);
        result.PaymentInfo.StripeSessionId.Should().Be("cs_new");
        result.PaymentInfo.StripePaymentIntentId.Should().Be("pi_paid");
        result.PaymentInfo.StripeChargeId.Should().Be("ch_paid");
        result.PaymentInfo.FailureReason.Should().BeNull();
    }

    [Fact]
    public async Task Handle_OrderSummaryIsStale_ResolvesRefundedStatusFromPaymentHistory()
    {
        var ownerId = Guid.NewGuid();
        var order = BuildStripeOrder(ownerId);
        var payment = PaymentEntity.Create(order.Id, "cs_refunded", 45_000m, "vnd");
        payment.MarkSucceeded("pi_refunded", "ch_refunded");
        payment.AddRefund(RefundRecord.Create(payment.Id, "re_refunded", 45_000m, Guid.NewGuid(), "sync"));
        payment.Refunds.Single().MarkSucceeded();

        _ordersRepo.Setup(r => r.GetByIdWithItemsAsync(order.Id)).ReturnsAsync(order);
        _paymentsRepo
            .Setup(r => r.ListByOrderIdAsync(order.Id))
            .ReturnsAsync([payment]);

        var result = await _handler.Handle(
            new GetOrderByIdQuery(order.Id, ownerId, false),
            CancellationToken.None);

        result.PaymentInfo.PaymentStatus.Should().Be(EPaymentStatus.Refunded);
    }

    private static OrderEntity BuildOrder(Guid userId, Guid? productId = null)
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem>
        {
            OrderItem.Create(productId ?? Guid.NewGuid(), "Cà phê sữa", 45_000, 1, null, null)
        };
        return OrderEntity.Create("MRC-TEST-001", userId, delivery, items, EPaymentMethod.COD);
    }

    private static OrderEntity BuildStripeOrder(Guid userId)
    {
        var delivery = new DeliveryInfo("Test User", "0901234567", "123 Test St");
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "A-Mê Classic", 45_000, 1, null, null)
        };

        var order = OrderEntity.Create("MRC-TEST-002", userId, delivery, items, EPaymentMethod.STRIPE);
        order.MarkPaymentPaid("pi_stale_summary", "ch_stale_summary");
        return order;
    }
}
