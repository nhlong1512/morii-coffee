using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.Commands.Payment.CreateVnpayPaymentUrl;

public sealed class CreateVnpayPaymentUrlCommandHandler
    : ICommandHandler<CreateVnpayPaymentUrlCommand, VnpayPaymentUrlResponseDto>
{
    private readonly ICartService _cartService;
    private readonly IPaymentGatewayResolver _resolver;
    private readonly IStripeCheckoutDraftService _draftService;
    private readonly VnpaySettings _settings;
    private readonly ILogger<CreateVnpayPaymentUrlCommandHandler> _logger;

    public CreateVnpayPaymentUrlCommandHandler(
        ICartService cartService,
        IPaymentGatewayResolver resolver,
        IStripeCheckoutDraftService draftService,
        VnpaySettings settings,
        ILogger<CreateVnpayPaymentUrlCommandHandler> logger)
    {
        _cartService = cartService;
        _resolver = resolver;
        _draftService = draftService;
        _settings = settings;
        _logger = logger;
    }

    public async Task<VnpayPaymentUrlResponseDto> Handle(
        CreateVnpayPaymentUrlCommand command,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetCartAsync(command.UserId);
        if (cart.Items.Count == 0)
            throw new BadRequestException("Cart is empty.");

        var draftId = Guid.NewGuid();
        var amount = cart.Items.Sum(item => item.UnitPrice * item.Quantity) + (command.ShippingFee ?? 0);
        var gateway = _resolver.Resolve(EPaymentProvider.Vnpay);
        var result = await gateway.CreateCheckoutSessionAsync(new CreateCheckoutSessionRequest
        {
            ClientReferenceId = draftId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["checkoutDraftId"] = draftId.ToString(),
                ["userId"] = command.UserId.ToString(),
                ["ipAddress"] = command.IpAddress
            },
            TotalAmount = (long)amount,
            Currency = _settings.Currency,
            Items = cart.Items.Select(item => new CheckoutLineItem
            {
                Name = item.ProductName,
                UnitAmount = (long)item.UnitPrice,
                Quantity = item.Quantity
            }).ToList(),
            SuccessUrl = _settings.ReturnUrl,
            CancelUrl = _settings.ReturnUrl
        }, cancellationToken);

        await _draftService.StoreAsync(new StripeCheckoutDraftCacheDto
        {
            PaymentMethod = EPaymentMethod.VNPAY,
            Provider = EPaymentProvider.Vnpay,
            DraftId = draftId,
            UserId = command.UserId,
            FullName = command.FullName.Trim(),
            PhoneNumber = command.PhoneNumber.Trim(),
            Address = command.Address.Trim(),
            ProvinceId = command.ProvinceId,
            ProvinceName = command.ProvinceName?.Trim(),
            DistrictId = command.DistrictId,
            DistrictName = command.DistrictName?.Trim(),
            WardCode = command.WardCode?.Trim(),
            WardName = command.WardName?.Trim(),
            Notes = command.Notes?.Trim(),
            SaveDeliveryProfile = command.SaveDeliveryProfile,
            DeliveryMethod = command.DeliveryMethod,
            ShippingQuoteFingerprint = command.ShippingQuoteFingerprint?.Trim(),
            ShippingServiceId = command.ShippingServiceId,
            ShippingServiceTypeId = command.ShippingServiceTypeId,
            ShippingServiceLabel = command.ShippingServiceLabel?.Trim(),
            ShippingFee = command.ShippingFee,
            ShippingQuoteExpiresAt = command.ShippingQuoteExpiresAt,
            ShippingProviderEnvironment = command.ShippingProviderEnvironment?.Trim(),
            Items = cart.Items.Select(Clone).ToList(),
            Amount = amount,
            Currency = _settings.Currency.ToLowerInvariant(),
            SessionId = result.SessionId,
            ExpiresAtUtc = result.ExpiresAtUtc
        });

        _logger.LogInformation("Created VNPAY payment URL for draft {DraftId}", draftId);
        return new VnpayPaymentUrlResponseDto
        {
            CheckoutDraftId = draftId,
            TxnRef = result.SessionId,
            PaymentUrl = result.Url,
            Amount = (long)amount,
            Currency = _settings.Currency,
            ExpiresAtUtc = result.ExpiresAtUtc
        };
    }

    private static CartItemDto Clone(CartItemDto item) => new()
    {
        ProductId = item.ProductId,
        VariantId = item.VariantId,
        VariantLabel = item.VariantLabel,
        ProductName = item.ProductName,
        UnitPrice = item.UnitPrice,
        Quantity = item.Quantity,
        ImageUrl = item.ImageUrl,
        AddedAt = item.AddedAt
    };
}
