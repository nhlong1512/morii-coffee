using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Shared.Settings;
using Xunit;

namespace MoriiCoffee.Application.Tests.Commands.Payment;

public class CreateCheckoutSessionCommandHandlerTests
{
    private readonly Mock<ICartService> _cartService = new();
    private readonly Mock<IPaymentGateway> _gateway = new();
    private readonly Mock<IStripeCheckoutDraftService> _draftService = new();

    private readonly StripeSettings _stripeSettings = new()
    {
        SecretKey = "sk_test_dummy",
        PublishableKey = "pk_test_dummy",
        WebhookSigningSecret = "whsec_dummy",
        Currency = "vnd",
        SuccessUrlTemplate = "/checkout/success?session_id={CHECKOUT_SESSION_ID}",
        CancelUrlPath = "/checkout/cancel"
    };

    private readonly EmailSettings _emailSettings = new()
    {
        StorefrontUrl = "http://localhost:3000"
    };

    private readonly CreateCheckoutSessionCommandHandler _handler;

    public CreateCheckoutSessionCommandHandlerTests()
    {
        _gateway.SetupGet(g => g.PublishableKey).Returns("pk_test_dummy");

        _handler = new CreateCheckoutSessionCommandHandler(
            _cartService.Object,
            _gateway.Object,
            _draftService.Object,
            _stripeSettings,
            _emailSettings,
            NullLogger<CreateCheckoutSessionCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_EmptyCart_ThrowsBadRequest()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(new CartDto());

        var act = () => _handler.Handle(BuildCommand(userId), CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*Cart is empty*");
    }

    [Fact]
    public async Task Handle_HappyPath_CreatesStripeSessionAndStoresDraft()
    {
        var userId = Guid.NewGuid();
        var cart = BuildCart();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(cart);

        _gateway
            .Setup(g => g.CreateCheckoutSessionAsync(
                It.IsAny<CreateCheckoutSessionRequest>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CheckoutSessionResult
            {
                SessionId = "cs_test_happy",
                Url = "https://checkout.stripe.com/c/pay/cs_test_happy",
                ExpiresAtUtc = DateTime.UtcNow.AddHours(24)
            });

        StripeCheckoutDraftCacheDto? storedDraft = null;
        _draftService
            .Setup(s => s.StoreAsync(It.IsAny<StripeCheckoutDraftCacheDto>()))
            .Callback<StripeCheckoutDraftCacheDto>(draft => storedDraft = draft)
            .Returns(Task.CompletedTask);

        var result = await _handler.Handle(BuildCommand(userId), CancellationToken.None);

        result.SessionId.Should().Be("cs_test_happy");
        result.CheckoutUrl.Should().Be("https://checkout.stripe.com/c/pay/cs_test_happy");
        result.Amount.Should().Be(137_000);
        result.Currency.Should().Be("vnd");
        result.PublishableKey.Should().Be("pk_test_dummy");
        result.CheckoutDraftId.Should().NotBe(Guid.Empty);

        storedDraft.Should().NotBeNull();
        storedDraft!.DraftId.Should().Be(result.CheckoutDraftId);
        storedDraft.UserId.Should().Be(userId);
        storedDraft.SessionId.Should().Be("cs_test_happy");
        storedDraft.Amount.Should().Be(137_000m);
        storedDraft.Items.Should().HaveCount(2);
        storedDraft.ProvinceId.Should().Be(79);
        storedDraft.DistrictName.Should().Be("District 11");
        storedDraft.WardCode.Should().Be("01001");

        _gateway.Verify(g => g.CreateCheckoutSessionAsync(
            It.Is<CreateCheckoutSessionRequest>(request =>
                request.ClientReferenceId == result.CheckoutDraftId.ToString() &&
                request.Metadata["checkoutDraftId"] == result.CheckoutDraftId.ToString() &&
                request.Metadata["userId"] == userId.ToString() &&
                request.TotalAmount == 137_000 &&
                request.Items.Count == 2 &&
                request.SuccessUrl == "http://localhost:3000/checkout/success?session_id={CHECKOUT_SESSION_ID}" &&
                request.CancelUrl == "http://localhost:3000/checkout/cancel"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_GatewayThrows_DoesNotStoreDraft()
    {
        var userId = Guid.NewGuid();
        _cartService.Setup(c => c.GetCartAsync(userId)).ReturnsAsync(BuildCart());
        _gateway
            .Setup(g => g.CreateCheckoutSessionAsync(It.IsAny<CreateCheckoutSessionRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Stripe API down"));

        var act = () => _handler.Handle(BuildCommand(userId), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _draftService.Verify(s => s.StoreAsync(It.IsAny<StripeCheckoutDraftCacheDto>()), Times.Never);
    }

    private static CreateCheckoutSessionCommand BuildCommand(Guid userId) => new()
    {
        UserId = userId,
        FullName = "Hữu Long Nguyễn",
        PhoneNumber = "0775504619",
        Address = "1170/61 3 Tháng 2, ward 8, district 11",
        ProvinceId = 79,
        ProvinceName = "Ho Chi Minh",
        DistrictId = 771,
        DistrictName = "District 11",
        WardCode = "01001",
        WardName = "Ward 8",
        Notes = "No ice",
        SaveDeliveryProfile = true
    };

    private static CartDto BuildCart() => new()
    {
        Items =
        [
            new CartItemDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "Bạc Xỉu Caramel Muối",
                VariantId = Guid.NewGuid(),
                VariantLabel = "Lớn",
                UnitPrice = 49_000m,
                Quantity = 2
            },
            new CartItemDto
            {
                ProductId = Guid.NewGuid(),
                ProductName = "A-Mê Classic",
                VariantId = Guid.NewGuid(),
                VariantLabel = "Nhỏ",
                UnitPrice = 39_000m,
                Quantity = 1
            }
        ]
    };
}
