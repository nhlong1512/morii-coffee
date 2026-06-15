using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Helpers;
using MoriiCoffee.Application.Services.Shipping;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.ValueObjects;
using MoriiCoffee.Domain.Aggregates.UserAggregate.Entities;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Shipping;
using OrderEntity = MoriiCoffee.Domain.Aggregates.OrderAggregate.Order;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Services;

public class StripeCheckoutDraftService : IStripeCheckoutDraftService
{
    private static readonly TimeSpan MinimumDraftTtl = TimeSpan.FromHours(2);
    private static readonly TimeSpan DraftRetentionAfterExpiry = TimeSpan.FromHours(6);

    private readonly ICacheService _cacheService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOrderIdGenerator _orderIdGenerator;
    private readonly ICartService _cartService;
    private readonly ILogger<StripeCheckoutDraftService> _logger;
    private readonly ShipmentLifecycleService? _shipmentLifecycleService;

    public StripeCheckoutDraftService(
        ICacheService cacheService,
        IUnitOfWork unitOfWork,
        IOrderIdGenerator orderIdGenerator,
        ICartService cartService,
        ILogger<StripeCheckoutDraftService> logger,
        ShipmentLifecycleService? shipmentLifecycleService = null)
    {
        _cacheService = cacheService;
        _unitOfWork = unitOfWork;
        _orderIdGenerator = orderIdGenerator;
        _cartService = cartService;
        _logger = logger;
        _shipmentLifecycleService = shipmentLifecycleService;
    }

    public async Task StoreAsync(StripeCheckoutDraftCacheDto draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        var ttl = ComputeDraftTtl(draft.ExpiresAtUtc);
        await _cacheService.SetDataAsync(CachedKeyConstants.StripeCheckoutDraftById(draft.DraftId), draft, ttl);
        await _cacheService.SetDataAsync(CachedKeyConstants.StripeCheckoutDraftBySession(draft.SessionId), draft.DraftId, ttl);
    }

    public async Task<StripeCheckoutDraftCacheDto?> GetByDraftIdAsync(Guid draftId)
    {
        if (draftId == Guid.Empty)
            return null;

        return await _cacheService.GetDataAsync<StripeCheckoutDraftCacheDto>(
            CachedKeyConstants.StripeCheckoutDraftById(draftId));
    }

    public async Task<StripeCheckoutDraftCacheDto?> GetBySessionIdAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
            return null;

        var draftId = await _cacheService.GetDataAsync<Guid?>(
            CachedKeyConstants.StripeCheckoutDraftBySession(sessionId));

        if (draftId is null || draftId == Guid.Empty)
            return null;

        return await GetByDraftIdAsync(draftId.Value);
    }

    public async Task MarkFailedAsync(StripeCheckoutDraftCacheDto draft, string? failureReason)
    {
        draft.PaymentStatus = EPaymentStatus.Failed;
        draft.FailureReason = string.IsNullOrWhiteSpace(failureReason) ? null : failureReason.Trim();
        await StoreAsync(draft);
    }

    public async Task MarkExpiredAsync(StripeCheckoutDraftCacheDto draft)
    {
        draft.PaymentStatus = EPaymentStatus.Failed;
        draft.FailureReason = "Stripe checkout session expired before payment was completed.";
        await StoreAsync(draft);
    }

    public async Task<FinalizeStripeCheckoutResultDto> FinalizeSucceededAsync(
        StripeCheckoutDraftCacheDto draft,
        string paymentIntentId,
        string chargeId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(paymentIntentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(chargeId);

        var existingPayment = await _unitOfWork.Payments.GetBySessionIdAsync(draft.SessionId, draft.Provider);
        if (existingPayment is not null)
            return await BuildExistingResultAsync(existingPayment);

        try
        {
            FinalizeStripeCheckoutResultDto? result = null;

            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var orderNumber = await _orderIdGenerator.GenerateAsync();
                var orderItems = draft.Items.Select(item => OrderItem.Create(
                    item.ProductId,
                    item.ProductName,
                    item.UnitPrice,
                    item.Quantity,
                    item.VariantId,
                    item.VariantLabel)).ToList();

                var order = OrderEntity.Create(
                    orderNumber,
                    draft.UserId,
                    new DeliveryInfo(
                        draft.FullName,
                        draft.PhoneNumber,
                        draft.Address,
                        draft.ProvinceId,
                        draft.ProvinceName,
                        draft.DistrictId,
                        draft.DistrictName,
                        draft.WardCode,
                        draft.WardName),
                    orderItems,
                    draft.PaymentMethod,
                    draft.Notes,
                    deliveryMethod: draft.DeliveryMethod);

                if (draft.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY)
                {
                    order.ApplyShippingQuote(
                        EShippingProvider.GHN,
                        draft.ShippingQuoteFingerprint!,
                        draft.ShippingServiceId!.Value,
                        draft.ShippingServiceTypeId,
                        draft.ShippingServiceLabel,
                        draft.ShippingProviderEnvironment!,
                        draft.ShippingQuoteExpiresAt!.Value,
                        draft.ShippingFee ?? 0);
                }

                order.MarkPaymentPaid(paymentIntentId, chargeId);
                await _unitOfWork.Orders.CreateAsync(order);

                var payment = PaymentEntity.Create(
                    order.Id,
                    draft.SessionId,
                    draft.Amount,
                    draft.Currency,
                    provider: draft.Provider);
                payment.MarkSucceeded(paymentIntentId, chargeId);
                await _unitOfWork.Payments.CreateAsync(payment);

                if (draft.SaveDeliveryProfile)
                    await UpsertDeliveryProfileAsync(draft);

                result = new FinalizeStripeCheckoutResultDto
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    PaymentId = payment.Id,
                    PaymentStatus = PaymentStatusResolver.Resolve(order, [payment]),
                    SessionId = draft.SessionId
                };
            });

            if (draft.DeliveryMethod == EDeliveryMethod.GHN_DELIVERY && _shipmentLifecycleService is not null)
            {
                var createdOrder = await _unitOfWork.Orders.GetByIdWithItemsAsync(result!.OrderId);
                if (createdOrder is not null)
                    await _shipmentLifecycleService.TryCreateForOrderAsync(createdOrder, cancellationToken);
            }

            await CleanupAfterSuccessAsync(draft);
            return result!;
        }
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(
                ex,
                "Stripe checkout draft {DraftId} raced during finalization for session {SessionId}; reloading committed state.",
                draft.DraftId,
                draft.SessionId);

            var committedPayment = await _unitOfWork.Payments.GetBySessionIdAsync(draft.SessionId, draft.Provider);
            if (committedPayment is not null)
            {
                await CleanupAfterSuccessAsync(draft);
                return await BuildExistingResultAsync(committedPayment);
            }

            throw;
        }
    }

    private async Task<FinalizeStripeCheckoutResultDto> BuildExistingResultAsync(PaymentEntity payment)
    {
        var order = await _unitOfWork.Orders.GetByIdAsync(payment.OrderId)
            ?? throw new InvalidOperationException(
                $"Payment {payment.Id} exists for Stripe session {payment.StripeSessionId} but its order is missing.");

        return new FinalizeStripeCheckoutResultDto
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            PaymentId = payment.Id,
            PaymentStatus = PaymentStatusResolver.Resolve(order, [payment]),
            SessionId = payment.StripeSessionId
        };
    }

    private async Task UpsertDeliveryProfileAsync(StripeCheckoutDraftCacheDto draft)
    {
        var existingProfile = await _unitOfWork.UserDeliveryProfiles.GetByUserIdAsync(draft.UserId);

        if (existingProfile is null)
        {
            var newProfile = UserDeliveryProfile.Create(
                draft.UserId,
                draft.FullName,
                draft.PhoneNumber,
                draft.Address,
                draft.ProvinceId,
                draft.ProvinceName,
                draft.DistrictId,
                draft.DistrictName,
                draft.WardCode,
                draft.WardName);
            await _unitOfWork.UserDeliveryProfiles.UpsertAsync(newProfile);
            return;
        }

        existingProfile.Update(
            draft.FullName,
            draft.PhoneNumber,
            draft.Address,
            draft.ProvinceId,
            draft.ProvinceName,
            draft.DistrictId,
            draft.DistrictName,
            draft.WardCode,
            draft.WardName);
        await _unitOfWork.UserDeliveryProfiles.UpsertAsync(existingProfile);
    }

    private async Task CleanupAfterSuccessAsync(StripeCheckoutDraftCacheDto draft)
    {
        try
        {
            await RemovePurchasedItemsFromCartAsync(draft);
            await RemoveDraftAsync(draft);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Non-critical cleanup failed after Stripe checkout draft {DraftId} finalized.",
                draft.DraftId);
        }
    }

    private async Task RemovePurchasedItemsFromCartAsync(StripeCheckoutDraftCacheDto draft)
    {
        var cart = await _cartService.GetCartAsync(draft.UserId);

        foreach (var draftItem in draft.Items)
        {
            var existingItem = cart.Items.FirstOrDefault(item =>
                item.ProductId == draftItem.ProductId &&
                item.VariantId == draftItem.VariantId);

            if (existingItem is null)
                continue;

            var remainingQuantity = existingItem.Quantity - draftItem.Quantity;
            await _cartService.UpdateQuantityAsync(
                draft.UserId,
                draftItem.ProductId,
                draftItem.VariantId,
                remainingQuantity);
        }
    }

    private async Task RemoveDraftAsync(StripeCheckoutDraftCacheDto draft)
    {
        await _cacheService.RemoveDataAsync(CachedKeyConstants.StripeCheckoutDraftById(draft.DraftId));
        await _cacheService.RemoveDataAsync(CachedKeyConstants.StripeCheckoutDraftBySession(draft.SessionId));
    }

    private static TimeSpan ComputeDraftTtl(DateTime expiresAtUtc)
    {
        var ttl = expiresAtUtc - DateTime.UtcNow + DraftRetentionAfterExpiry;
        return ttl < MinimumDraftTtl ? MinimumDraftTtl : ttl;
    }
}
