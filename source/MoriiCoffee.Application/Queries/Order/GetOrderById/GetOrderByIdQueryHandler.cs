using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.DTOs.Shipping;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;
using MoriiCoffee.Domain.Shared.Settings;
using Microsoft.EntityFrameworkCore;
using OrderDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderDto;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using OrderItemDto = MoriiCoffee.Application.SeedWork.DTOs.Order.OrderItemDto;

namespace MoriiCoffee.Application.Queries.Order.GetOrderById;

/// <summary>
/// Handles <see cref="GetOrderByIdQuery"/> by loading the order with its items and enforcing
/// ownership rules for non-admin callers.
/// </summary>
public class GetOrderByIdQueryHandler : IQueryHandler<GetOrderByIdQuery, OrderDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly string? _cdnBaseUrl;

    /// <summary>Initialises the handler with its required dependency.</summary>
    public GetOrderByIdQueryHandler(IUnitOfWork unitOfWork, AwsS3Settings s3Settings)
    {
        _unitOfWork = unitOfWork;
        _cdnBaseUrl = s3Settings.CdnBaseUrl;
    }

    /// <inheritdoc />
    public async Task<OrderDto> Handle(GetOrderByIdQuery request, CancellationToken cancellationToken)
    {
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(request.OrderId);

        if (order is null)
            throw new NotFoundException("Order", request.OrderId);

        if (!request.IsAdmin && order.UserId != request.RequestingUserId)
            throw new UnauthorizedException("You are not authorized to view this order.");

        return await MapToDtoAsync(order, cancellationToken);
    }

    /// <summary>Maps an <see cref="OrderEntity"/> aggregate to its full <see cref="OrderDto"/> representation.</summary>
    private async Task<OrderDto> MapToDtoAsync(OrderEntity order, CancellationToken cancellationToken)
    {
        var productIds = order.Items
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        var productImageMap = productIds.Count == 0
            ? new Dictionary<Guid, string?>()
            : await _unitOfWork.Products
                .FindByCondition(p => productIds.Contains(p.Id), false)
                .ToDictionaryAsync(p => p.Id, p => p.ThumbnailUrl, cancellationToken);

        var payments = await _unitOfWork.Payments.ListByOrderIdAsync(order.Id);
        var latestPayment = payments.FirstOrDefault();
        var paymentStatus = PaymentStatusResolver.Resolve(order, payments);
        var shipment = await _unitOfWork.Shipments.GetByOrderIdAsync(order.Id);

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
                PaymentStatus = paymentStatus,
                AttemptCount = payments.Count,
                LatestPaymentId = latestPayment?.Id,
                LatestAttemptStatus = latestPayment?.Status,
                Provider = latestPayment?.Provider,
                StripeSessionId = latestPayment?.StripeSessionId,
                StripePaymentIntentId = latestPayment?.StripePaymentIntentId ?? order.StripePaymentIntentId,
                StripeChargeId = latestPayment?.StripeChargeId ?? order.StripeChargeId,
                FailureReason = latestPayment?.FailureReason,
                LatestAttemptCreatedAt = latestPayment?.CreatedAt
            },
            CreatedAt = order.CreatedAt,
            UpdatedAt = order.UpdatedAt,
            Shipment = shipment is null ? null : new ShipmentSummaryDto
            {
                Id = shipment.Id,
                Provider = shipment.Provider,
                ProviderEnvironment = shipment.ProviderEnvironment,
                Status = shipment.Status,
                StatusLabel = shipment.StatusLabel,
                ClientOrderCode = shipment.ClientOrderCode,
                ProviderOrderCode = shipment.ProviderOrderCode,
                ShopId = shipment.ShopId,
                ServiceId = shipment.ServiceId,
                ServiceTypeId = shipment.ServiceTypeId,
                FeeTotal = shipment.FeeTotal,
                ExpectedDeliveryAt = shipment.ExpectedDeliveryAt,
                TrackingUrl = shipment.TrackingUrl,
                FailureReasonCode = shipment.FailureReasonCode,
                FailureReason = shipment.FailureReason,
                Note = shipment.Note,
                LastSyncedAt = shipment.LastSyncedAt
            },
            Items = order.Items.Select(i => new OrderItemDto
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                VariantId = i.VariantId,
                VariantLabel = i.VariantLabel,
                ImageUrl = CdnUrlHelper.Resolve(productImageMap.GetValueOrDefault(i.ProductId), _cdnBaseUrl),
                UnitPrice = i.UnitPrice,
                Quantity = i.Quantity,
                LineTotal = i.LineTotal
            }).ToList()
        };
    }
}
