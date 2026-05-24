using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Application.Services;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Tests.Services;

public class StripeCheckoutDraftServiceTests
{
    private readonly Mock<ICacheService> _cacheService = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderIdGenerator> _orderIdGenerator = new();
    private readonly Mock<ICartService> _cartService = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IUserDeliveryProfileRepository> _profilesRepo = new();
    private readonly StripeCheckoutDraftService _service;

    public StripeCheckoutDraftServiceTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _unitOfWork.Setup(u => u.UserDeliveryProfiles).Returns(_profilesRepo.Object);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(fn => fn());

        _service = new StripeCheckoutDraftService(
            _cacheService.Object,
            _unitOfWork.Object,
            _orderIdGenerator.Object,
            _cartService.Object,
            NullLogger<StripeCheckoutDraftService>.Instance);
    }

    [Fact]
    public async Task FinalizeSucceededAsync_CreatesOrderPayment_UpdatesCart_AndRemovesDraft()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var draft = BuildDraft(userId, productId, variantId);
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260518-001");
        _paymentsRepo.Setup(r => r.GetBySessionIdAsync(draft.SessionId)).ReturnsAsync((PaymentEntity?)null);
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserDeliveryProfile?)null);
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(new CartDto
        {
            Items =
            [
                new CartItemDto
                {
                    ProductId = productId,
                    VariantId = variantId,
                    ProductName = "A-Mê Đào",
                    Quantity = 3,
                    UnitPrice = 49_000m
                }
            ]
        });

        OrderEntity? createdOrder = null;
        PaymentEntity? createdPayment = null;
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>()))
            .Callback<OrderEntity>(order => createdOrder = order)
            .Returns(Task.CompletedTask);
        _paymentsRepo.Setup(r => r.CreateAsync(It.IsAny<PaymentEntity>()))
            .Callback<PaymentEntity>(payment => createdPayment = payment)
            .Returns(Task.CompletedTask);
        _cacheService.Setup(c => c.RemoveDataAsync(It.IsAny<string>())).ReturnsAsync(true);
        _cacheService.Setup(c => c.SetDataAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()))
            .ReturnsAsync(true);

        var result = await _service.FinalizeSucceededAsync(
            draft,
            "pi_paid",
            "ch_paid",
            CancellationToken.None);

        result.OrderNumber.Should().Be("MRC-20260518-001");
        result.PaymentStatus.Should().Be(EPaymentStatus.Paid);
        createdOrder.Should().NotBeNull();
        createdOrder!.PaymentStatus.Should().Be(EPaymentStatus.Paid);
        createdPayment.Should().NotBeNull();
        createdPayment!.Status.Should().Be(EPaymentTransactionStatus.Succeeded);

        _cartService.Verify(c => c.UpdateQuantityAsync(userId, productId, variantId, 2), Times.Once);
        _cacheService.Verify(c => c.RemoveDataAsync(CachedKeyConstants.StripeCheckoutDraftById(draft.DraftId)), Times.Once);
        _cacheService.Verify(c => c.RemoveDataAsync(CachedKeyConstants.StripeCheckoutDraftBySession(draft.SessionId)), Times.Once);
        _profilesRepo.Verify(r => r.UpsertAsync(It.Is<UserDeliveryProfile>(p => p.UserId == userId)), Times.Once);
    }

    [Fact]
    public async Task FinalizeSucceededAsync_WhenPaymentAlreadyExists_ReturnsExistingState()
    {
        var draft = BuildDraft(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var existingOrder = OrderEntity.Create(
            "MRC-20260518-099",
            draft.UserId,
            new MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects.DeliveryInfo(
                draft.FullName,
                draft.PhoneNumber,
                draft.Address),
            [MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities.OrderItem.Create(draft.Items[0].ProductId, draft.Items[0].ProductName, draft.Items[0].UnitPrice, draft.Items[0].Quantity, draft.Items[0].VariantId, draft.Items[0].VariantLabel)],
            EPaymentMethod.STRIPE,
            draft.Notes);
        existingOrder.MarkPaymentPaid("pi_existing", "ch_existing");
        var existingPayment = PaymentEntity.Create(existingOrder.Id, draft.SessionId, draft.Amount, draft.Currency);
        existingPayment.MarkSucceeded("pi_existing", "ch_existing");

        _paymentsRepo.Setup(r => r.GetBySessionIdAsync(draft.SessionId)).ReturnsAsync(existingPayment);
        _ordersRepo.Setup(r => r.GetByIdAsync(existingOrder.Id)).ReturnsAsync(existingOrder);

        var result = await _service.FinalizeSucceededAsync(
            draft,
            "pi_existing",
            "ch_existing",
            CancellationToken.None);

        result.OrderId.Should().Be(existingOrder.Id);
        result.PaymentId.Should().Be(existingPayment.Id);
        _ordersRepo.Verify(r => r.CreateAsync(It.IsAny<OrderEntity>()), Times.Never);
        _paymentsRepo.Verify(r => r.CreateAsync(It.IsAny<PaymentEntity>()), Times.Never);
    }

    [Fact]
    public async Task FinalizeSucceededAsync_GhnDraft_PreservesShippingSnapshot()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variantId = Guid.NewGuid();
        var draft = BuildDraft(userId, productId, variantId);
        draft.DeliveryMethod = EDeliveryMethod.GHN_DELIVERY;
        draft.ShippingServiceId = 53320;
        draft.ShippingServiceTypeId = 2;
        draft.ShippingServiceLabel = "GHN Chuẩn";
        draft.ShippingFee = 15_000m;
        draft.ShippingQuoteExpiresAt = DateTime.UtcNow.AddMinutes(15);
        draft.ShippingProviderEnvironment = "sandbox";
        draft.ProvinceId = 79;
        draft.ProvinceName = "Ho Chi Minh";
        draft.DistrictId = 760;
        draft.DistrictName = "District 1";
        draft.WardCode = "26734";
        draft.WardName = "Ben Nghe";
        draft.ShippingQuoteFingerprint = new ShippingQuoteFingerprintService().Generate(
            EDeliveryMethod.GHN_DELIVERY,
            EPaymentMethod.STRIPE,
            new MoriiCoffee.Application.SeedWork.DTOs.Shipping.ShippingAddressDto
            {
                FullName = draft.FullName,
                PhoneNumber = draft.PhoneNumber,
                AddressLine = draft.Address,
                ProvinceId = draft.ProvinceId,
                ProvinceName = draft.ProvinceName,
                DistrictId = draft.DistrictId,
                DistrictName = draft.DistrictName,
                WardCode = draft.WardCode,
                WardName = draft.WardName
            },
            new ShippingPackageMetricsService().BuildFromCart(draft.Items),
            draft.ShippingServiceId.Value,
            draft.ShippingServiceTypeId,
            draft.ShippingQuoteExpiresAt.Value);

        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260518-010");
        _paymentsRepo.Setup(r => r.GetBySessionIdAsync(draft.SessionId)).ReturnsAsync((PaymentEntity?)null);
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserDeliveryProfile?)null);
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(new CartDto { Items = [] });

        OrderEntity? createdOrder = null;
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>()))
            .Callback<OrderEntity>(order => createdOrder = order)
            .Returns(Task.CompletedTask);
        _paymentsRepo.Setup(r => r.CreateAsync(It.IsAny<PaymentEntity>())).Returns(Task.CompletedTask);
        _cacheService.Setup(c => c.RemoveDataAsync(It.IsAny<string>())).ReturnsAsync(true);

        await _service.FinalizeSucceededAsync(draft, "pi_shipping", "ch_shipping", CancellationToken.None);

        createdOrder.Should().NotBeNull();
        createdOrder!.DeliveryMethod.Should().Be(EDeliveryMethod.GHN_DELIVERY);
        createdOrder.ShippingProvider.Should().Be(EShippingProvider.GHN);
        createdOrder.ShippingServiceId.Should().Be(53320);
        createdOrder.Shipping.Should().Be(15_000m);
    }

    private static StripeCheckoutDraftCacheDto BuildDraft(Guid userId, Guid productId, Guid variantId) => new()
    {
        DraftId = Guid.NewGuid(),
        UserId = userId,
        FullName = "Hữu Long Nguyễn",
        PhoneNumber = "0775504619",
        Address = "1170/61 3 Tháng 2, ward 8, district 11",
        Notes = "No ice",
        SaveDeliveryProfile = true,
        DeliveryMethod = MoriiCoffee.Domain.Shared.Enums.Shipping.EDeliveryMethod.PICKUP,
        Items =
        [
            new CartItemDto
            {
                ProductId = productId,
                VariantId = variantId,
                VariantLabel = "Nhỏ",
                ProductName = "A-Mê Đào",
                Quantity = 1,
                UnitPrice = 49_000m
            }
        ],
        Amount = 49_000m,
        Currency = "vnd",
        SessionId = "cs_test_service",
        ExpiresAtUtc = DateTime.UtcNow.AddHours(1)
    };
}
