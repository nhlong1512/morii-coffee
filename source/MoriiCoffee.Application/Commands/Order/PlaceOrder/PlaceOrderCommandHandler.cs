using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using OrderDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderDto;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using OrderItemDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderItemDto;

namespace MoriiCoffee.Application.Commands.Order.PlaceOrder;

/// <summary>
/// Handles <see cref="PlaceOrderCommand"/> by converting the user's cart into a persisted order,
/// optionally saving the delivery profile, and clearing the cart on success.
/// </summary>
public class PlaceOrderCommandHandler : ICommandHandler<PlaceOrderCommand, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICartService _cartService;
    private readonly IOrderIdGenerator _orderIdGenerator;
    private readonly string? _cdnBaseUrl;
    private readonly ShippingPackageMetricsService _packageMetricsService;
    private readonly ShippingQuoteValidationService _quoteValidationService;
    private readonly ShipmentLifecycleService? _shipmentLifecycleService;

    /// <summary>Initialises the handler with its required dependencies.</summary>
    public PlaceOrderCommandHandler(
        IUnitOfWork unitOfWork,
        ICartService cartService,
        IOrderIdGenerator orderIdGenerator,
        AwsS3Settings s3Settings,
        ShipmentLifecycleService? shipmentLifecycleService = null,
        ShippingPackageMetricsService? packageMetricsService = null,
        ShippingQuoteValidationService? quoteValidationService = null)
    {
        _unitOfWork = unitOfWork;
        _cartService = cartService;
        _orderIdGenerator = orderIdGenerator;
        _cdnBaseUrl = s3Settings.CdnBaseUrl;
        _shipmentLifecycleService = shipmentLifecycleService;
        _packageMetricsService = packageMetricsService ?? new ShippingPackageMetricsService();
        _quoteValidationService = quoteValidationService
            ?? new ShippingQuoteValidationService(new ShippingQuoteFingerprintService());
    }

    /// <inheritdoc />
    public async Task<OrderDto> Handle(PlaceOrderCommand command, CancellationToken cancellationToken)
    {
        if (command.PaymentMethod == MoriiCoffee.Domain.Shared.Enums.Order.EPaymentMethod.STRIPE)
            throw new BadRequestException(
                "Stripe checkout now uses a payment-first flow. Use POST /api/v1/payments/stripe/checkout-session instead of creating an order directly.");

        // 1. Load current cart
        var cart = await _cartService.GetCartAsync(command.UserId);

        if (cart.Items.Count == 0)
            throw new BadRequestException("Cart is empty.");

        // 2. Build order items from cart snapshot (prices already validated at add-to-cart time)
        var orderItems = cart.Items
            .Select(item => OrderItem.Create(
                item.ProductId,
                item.ProductName,
                item.UnitPrice,
                item.Quantity,
                item.VariantId,
                item.VariantLabel))
            .ToList();

        // 3. Generate order number
        var orderNumber = await _orderIdGenerator.GenerateAsync();

        if (command.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY)
        {
            var packageMetrics = _packageMetricsService.BuildFromCart(cart.Items);
            _quoteValidationService.EnsureValid(
                BuildQuoteFromCommand(command),
                command.DeliveryMethod,
                command.PaymentMethod,
                BuildAddress(command),
                packageMetrics);
        }

        // 4. Create domain objects
        var deliveryInfo = new DeliveryInfo(
            command.FullName,
            command.PhoneNumber,
            command.Address,
            command.ProvinceId,
            command.ProvinceName,
            command.DistrictId,
            command.DistrictName,
            command.WardCode,
            command.WardName);

        var order = OrderEntity.Create(
            orderNumber,
            command.UserId,
            deliveryInfo,
            orderItems,
            command.PaymentMethod,
            command.Notes,
            deliveryMethod: command.DeliveryMethod);

        if (command.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY)
        {
            order.ApplyShippingQuote(
                EShippingProvider.GHN,
                command.ShippingQuoteFingerprint!,
                command.ShippingServiceId!.Value,
                command.ShippingServiceTypeId,
                command.ShippingServiceLabel,
                command.ShippingProviderEnvironment!,
                command.ShippingQuoteExpiresAt!.Value,
                command.ShippingFee ?? 0);
        }

        // 5. Persist inside a transaction
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _unitOfWork.Orders.CreateAsync(order);

            if (command.SaveDeliveryProfile)
            {
                var existingProfile = await _unitOfWork.UserDeliveryProfiles.GetByUserIdAsync(command.UserId);

                if (existingProfile is null)
                {
                    var newProfile = UserDeliveryProfile.Create(
                        command.UserId,
                        command.FullName,
                        command.PhoneNumber,
                        command.Address,
                        command.ProvinceId,
                        command.ProvinceName,
                        command.DistrictId,
                        command.DistrictName,
                        command.WardCode,
                        command.WardName);
                    await _unitOfWork.UserDeliveryProfiles.UpsertAsync(newProfile);
                }
                else
                {
                    existingProfile.Update(
                        command.FullName,
                        command.PhoneNumber,
                        command.Address,
                        command.ProvinceId,
                        command.ProvinceName,
                        command.DistrictId,
                        command.DistrictName,
                        command.WardCode,
                        command.WardName);
                    await _unitOfWork.UserDeliveryProfiles.UpsertAsync(existingProfile);
                }
            }
        });

        ShipmentSummaryDto? shipmentSummary = null;
        if (command.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY && _shipmentLifecycleService is not null)
        {
            var shipment = await _shipmentLifecycleService.TryCreateForOrderAsync(order, cancellationToken);
            shipmentSummary = shipment is null ? null : ShipmentLifecycleService.ToSummaryDto(shipment);
        }

        // 6. Clear cart after successful transaction
        await _cartService.ClearCartAsync(command.UserId);

        var result = await MapToDtoAsync(order, cart.Items, cancellationToken);
        result.Shipment = shipmentSummary;
        return result;
    }

    /// <summary>Maps an <see cref="OrderEntity"/> aggregate to its full <see cref="OrderDto"/> representation.</summary>
    private async Task<OrderDto> MapToDtoAsync(
        OrderEntity order,
        IReadOnlyCollection<MoriiCoffee.Application.SeedWork.DTOs.Cart.CartItemDto> cartItems,
        CancellationToken cancellationToken)
    {
        var cartItemImageMap = cartItems.ToDictionary(
            item => (item.ProductId, item.VariantId),
            item => CdnUrlHelper.Resolve(item.ImageUrl, _cdnBaseUrl));

        var missingProductIds = order.Items
            .Where(item => !cartItemImageMap.TryGetValue((item.ProductId, item.VariantId), out var imageUrl)
                || string.IsNullOrWhiteSpace(imageUrl))
            .Select(item => item.ProductId)
            .Distinct()
            .ToList();

        var productImageMap = missingProductIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await _unitOfWork.Products
                .FindByCondition(p => missingProductIds.Contains(p.Id), false)
                .ToDictionaryAsync(p => p.Id, p => p.ThumbnailUrl, cancellationToken);

        return new OrderDto
        {
            Id = order.Id,
            OrderNumber = order.OrderNumber,
            UserId = order.UserId,
            DeliveryFullName = order.DeliveryInfo.FullName,
            DeliveryPhoneNumber = order.DeliveryInfo.PhoneNumber,
            DeliveryAddress = order.DeliveryInfo.Address,
            DeliveryProvinceId = order.DeliveryInfo.ProvinceId,
            DeliveryProvinceName = order.DeliveryInfo.ProvinceName,
            DeliveryDistrictId = order.DeliveryInfo.DistrictId,
            DeliveryDistrictName = order.DeliveryInfo.DistrictName,
            DeliveryWardCode = order.DeliveryInfo.WardCode,
            DeliveryWardName = order.DeliveryInfo.WardName,
            DeliveryMethod = order.DeliveryMethod,
            ShippingProvider = order.ShippingProvider,
            ShippingQuoteFingerprint = order.ShippingQuoteFingerprint,
            ShippingServiceId = order.ShippingServiceId,
            ShippingServiceTypeId = order.ShippingServiceTypeId,
            ShippingServiceLabel = order.ShippingServiceLabel,
            ShippingQuoteExpiresAt = order.ShippingQuoteExpiresAt,
            ShippingProviderEnvironment = order.ShippingProviderEnvironment,
            Notes = order.Notes,
            PaymentMethod = order.PaymentMethod,
            Subtotal = order.Subtotal,
            Tax = order.Tax,
            Shipping = order.Shipping,
            Discount = order.Discount,
            Total = order.Total,
            OrderStatus = order.OrderStatus,
            PaymentInfo = new OrderPaymentInfoDto
            {
                PaymentStatus = PaymentStatusResolver.Resolve(order, []),
                AttemptCount = 0,
                LatestPaymentId = null,
                LatestAttemptStatus = null,
                StripeSessionId = null,
                StripePaymentIntentId = order.StripePaymentIntentId,
                StripeChargeId = order.StripeChargeId,
                FailureReason = null,
                LatestAttemptCreatedAt = null
            },
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                VariantId = i.VariantId,
                VariantLabel = i.VariantLabel,
                ImageUrl = cartItemImageMap.TryGetValue((i.ProductId, i.VariantId), out var imageUrl)
                    && !string.IsNullOrWhiteSpace(imageUrl)
                    ? imageUrl
                    : CdnUrlHelper.Resolve(productImageMap.GetValueOrDefault(i.ProductId), _cdnBaseUrl),
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }

    private static ShippingAddressDto BuildAddress(PlaceOrderCommand command) => new()
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
    };

    private static ShippingQuoteDto BuildQuoteFromCommand(PlaceOrderCommand command) => new()
    {
        Provider = EShippingProvider.GHN,
        Environment = command.ShippingProviderEnvironment ?? "sandbox",
        Address = BuildAddress(command),
        Service = new ShippingServiceOptionDto
        {
            ServiceId = command.ShippingServiceId ?? 0,
            ServiceTypeId = command.ShippingServiceTypeId,
            DisplayName = command.ShippingServiceLabel ?? $"GHN Service {command.ShippingServiceId}",
            ShortName = command.ShippingServiceLabel ?? $"GHN Service {command.ShippingServiceId}"
        },
        PackageMetrics = new ShippingPackageMetricsDto(),
        QuoteExpiresAt = command.ShippingQuoteExpiresAt ?? DateTime.UtcNow,
        QuoteFingerprint = command.ShippingQuoteFingerprint ?? string.Empty
    };
}
