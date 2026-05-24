using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Commands.Order.PlaceOrder;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using MoriiCoffee.Domain.Shared.Settings;
using MockQueryable;
using Xunit;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;

namespace MoriiCoffee.Application.Tests.Commands.Order;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<ICartService> _cartService = new();
    private readonly Mock<IOrderIdGenerator> _orderIdGenerator = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IUserDeliveryProfileRepository> _profilesRepo = new();
    private readonly Mock<IProductsRepository> _productsRepo = new();
    private readonly PlaceOrderCommandHandler _handler;
    private static readonly AwsS3Settings S3Settings = new() { CdnBaseUrl = "https://cdn.test" };

    public PlaceOrderCommandHandlerTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.UserDeliveryProfiles).Returns(_profilesRepo.Object);
        _unitOfWork.Setup(u => u.Products).Returns(_productsRepo.Object);
        _unitOfWork
            .Setup(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()))
            .Returns<Func<Task>>(fn => fn());
        _productsRepo
            .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<MoriiCoffee.Domain.Aggregates.ProductAggregate.Product, bool>>>(), false))
            .Returns(new List<MoriiCoffee.Domain.Aggregates.ProductAggregate.Product>().BuildMock());

        _handler = new PlaceOrderCommandHandler(
            _unitOfWork.Object,
            _cartService.Object,
            _orderIdGenerator.Object,
            S3Settings);
    }

    [Fact]
    public async Task Handle_CartIsEmpty_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId))
            .ReturnsAsync(new CartDto { Items = [] });

        var act = () => _handler.Handle(new PlaceOrderCommand { UserId = userId }, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*empty*");
    }

    [Fact]
    public async Task Handle_StripePaymentMethod_ThrowsBadRequestException()
    {
        var userId = Guid.NewGuid();

        var act = () => _handler.Handle(new PlaceOrderCommand
        {
            UserId = userId,
            FullName = "Nguyễn Văn A",
            PhoneNumber = "0901234567",
            Address = "123 Đường ABC, Quận 1",
            PaymentMethod = EPaymentMethod.STRIPE
        }, CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*payment-first flow*");
    }

    [Fact]
    public async Task Handle_ValidCart_CreatesOrderAndClearsCart()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260502-001");
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>())).Returns(Task.CompletedTask);

        var result = await _handler.Handle(BuildCommand(userId), CancellationToken.None);

        result.Should().NotBeNull();
        result.OrderNumber.Should().Be("MRC-20260502-001");
        result.PaymentInfo.PaymentStatus.Should().Be(EPaymentStatus.NotRequired);
        result.PaymentInfo.AttemptCount.Should().Be(0);
        result.DeliveryProvinceId.Should().Be(79);
        result.DeliveryDistrictName.Should().Be("District 1");
        _cartService.Verify(c => c.ClearCartAsync(userId), Times.Once);
        _unitOfWork.Verify(u => u.ExecuteInTransactionAsync(It.IsAny<Func<Task>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SaveDeliveryProfileTrue_NoExistingProfile_CreatesNewProfile()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260502-001");
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>())).Returns(Task.CompletedTask);
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync((UserDeliveryProfile?)null);

        await _handler.Handle(BuildCommand(userId, saveDeliveryProfile: true), CancellationToken.None);

        _profilesRepo.Verify(
            r => r.UpsertAsync(It.Is<UserDeliveryProfile>(p => p.UserId == userId && p.WardCode == "26734")),
            Times.Once);
    }

    [Fact]
    public async Task Handle_SaveDeliveryProfileTrue_ExistingProfile_UpdatesProfile()
    {
        var userId = Guid.NewGuid();
        var existing = UserDeliveryProfile.Create(userId, "Old Name", "0900000000", "Old Address");
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260502-001");
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>())).Returns(Task.CompletedTask);
        _profilesRepo.Setup(r => r.GetByUserIdAsync(userId)).ReturnsAsync(existing);

        var command = BuildCommand(userId, saveDeliveryProfile: true);
        await _handler.Handle(command, CancellationToken.None);

        _profilesRepo.Verify(
            r => r.UpsertAsync(It.Is<UserDeliveryProfile>(p => p.FullName == command.FullName && p.ProvinceName == command.ProvinceName)),
            Times.Once);
    }

    [Fact]
    public async Task Handle_CartItemMissingImage_FallsBackToResolvedProductThumbnail()
    {
        var userId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var cart = BuildCart(productId, imageUrl: null);
        var products = new List<MoriiCoffee.Domain.Aggregates.ProductAggregate.Product>
        {
            new()
            {
                Id = productId,
                ThumbnailUrl = "products/abc/123-photo.png"
            }
        };

        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(cart);
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260502-001");
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>())).Returns(Task.CompletedTask);
        _productsRepo
            .Setup(r => r.FindByCondition(It.IsAny<System.Linq.Expressions.Expression<Func<MoriiCoffee.Domain.Aggregates.ProductAggregate.Product, bool>>>(), false))
            .Returns(products.BuildMock());

        var result = await _handler.Handle(BuildCommand(userId), CancellationToken.None);

        result.Items.Single().ImageUrl.Should().Be("https://cdn.test/products/abc/123-photo.png");
    }

    [Fact]
    public async Task Handle_GhnDeliveryWithValidQuote_PersistsShippingSnapshot()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _orderIdGenerator.Setup(g => g.GenerateAsync()).ReturnsAsync("MRC-20260502-001");

        OrderEntity? createdOrder = null;
        _ordersRepo.Setup(r => r.CreateAsync(It.IsAny<OrderEntity>()))
            .Callback<OrderEntity>(order => createdOrder = order)
            .Returns(Task.CompletedTask);

        var command = BuildGhnCommand(userId);
        var result = await _handler.Handle(command, CancellationToken.None);

        createdOrder.Should().NotBeNull();
        createdOrder!.DeliveryMethod.Should().Be(EDeliveryMethod.GHN_DELIVERY);
        createdOrder.ShippingProvider.Should().Be(EShippingProvider.GHN);
        createdOrder.ShippingQuoteFingerprint.Should().Be(command.ShippingQuoteFingerprint);
        createdOrder.ShippingServiceId.Should().Be(command.ShippingServiceId);
        createdOrder.Shipping.Should().Be(command.ShippingFee);
        result.Shipping.Should().Be(command.ShippingFee!.Value);
        result.ShippingServiceId.Should().Be(command.ShippingServiceId);
    }

    private static CartDto BuildCart(Guid? productId = null, string? imageUrl = null) => new()
    {
        Items =
        [
            new CartItemDto
            {
                ProductId = productId ?? Guid.NewGuid(),
                ProductName = "Cà phê sữa",
                ImageUrl = imageUrl,
                UnitPrice = 45_000,
                Quantity = 2
            }
        ]
    };

    private static PlaceOrderCommand BuildCommand(Guid userId, bool saveDeliveryProfile = false) => new()
    {
        UserId = userId,
        FullName = "Nguyễn Văn A",
        PhoneNumber = "0901234567",
        Address = "123 Đường ABC, Quận 1",
        ProvinceId = 79,
        ProvinceName = "Ho Chi Minh",
        DistrictId = 760,
        DistrictName = "District 1",
        WardCode = "26734",
        WardName = "Ben Nghe",
        DeliveryMethod = MoriiCoffee.Domain.Shared.Enums.Shipping.EDeliveryMethod.PICKUP,
        PaymentMethod = EPaymentMethod.COD,
        SaveDeliveryProfile = saveDeliveryProfile
    };

    private static PlaceOrderCommand BuildGhnCommand(Guid userId)
    {
        var command = BuildCommand(userId);
        command.DeliveryMethod = EDeliveryMethod.GHN_DELIVERY;
        command.ShippingServiceId = 53320;
        command.ShippingServiceTypeId = 2;
        command.ShippingServiceLabel = "GHN Chuẩn";
        command.ShippingFee = 15_000m;
        command.ShippingQuoteExpiresAt = DateTime.UtcNow.AddMinutes(15);
        command.ShippingProviderEnvironment = "sandbox";

        var metrics = new ShippingPackageMetricsService().BuildFromCart(BuildCart().Items);
        command.ShippingQuoteFingerprint = new ShippingQuoteFingerprintService().Generate(
            command.DeliveryMethod,
            command.PaymentMethod,
            new MoriiCoffee.Application.SeedWork.DTOs.Shipping.ShippingAddressDto
            {
                FullName = command.FullName,
                PhoneNumber = command.PhoneNumber,
                AddressLine = command.Address,
                ProvinceId = command.ProvinceId,
                ProvinceName = command.ProvinceName,
                DistrictId = command.DistrictId,
                DistrictName = command.DistrictName,
                WardCode = command.WardCode,
                WardName = command.WardName
            },
            metrics,
            command.ShippingServiceId.Value,
            command.ShippingServiceTypeId,
            command.ShippingQuoteExpiresAt.Value);
        return command;
    }
}
