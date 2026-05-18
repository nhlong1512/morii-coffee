using MoriiCoffee.Application.SeedWork.DTOs.Payment;

namespace MoriiCoffee.Application.SeedWork.Abstractions;

public interface IStripeCheckoutDraftService
{
    Task StoreAsync(StripeCheckoutDraftCacheDto draft);

    Task<StripeCheckoutDraftCacheDto?> GetByDraftIdAsync(Guid draftId);

    Task<StripeCheckoutDraftCacheDto?> GetBySessionIdAsync(string sessionId);

    Task MarkFailedAsync(StripeCheckoutDraftCacheDto draft, string? failureReason);

    Task MarkExpiredAsync(StripeCheckoutDraftCacheDto draft);

    Task<FinalizeStripeCheckoutResultDto> FinalizeSucceededAsync(
        StripeCheckoutDraftCacheDto draft,
        string paymentIntentId,
        string chargeId,
        CancellationToken cancellationToken);
}
