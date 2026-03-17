using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.Shared.Settings;
using Serilog;
using Stripe;

namespace MoriiCoffee.Infrastructure.Services;

/// <summary>
/// Production Stripe service backed by the Stripe.net SDK.
/// Handles PaymentIntent creation, cancellation, and refunds.
/// Registered when <c>IStripeService</c> is resolved from DI.
/// </summary>
public class StripeService : IStripeService
{
    private readonly PaymentIntentService _intentService;
    private readonly RefundService _refundService;
    private readonly ILogger _logger;

    /// <summary>Initialises the Stripe SDK with the configured secret key.</summary>
    public StripeService(StripeSettings settings, ILogger logger)
    {
        StripeConfiguration.ApiKey = settings.SecretKey;
        _intentService = new PaymentIntentService();
        _refundService = new RefundService();
        _logger = logger;
    }

    /// <summary>
    /// Creates a Stripe PaymentIntent and returns the client secret for frontend confirmation.
    /// Amount is passed as-is (for VND: full amount; for USD: cents).
    /// </summary>
    public async Task<PaymentIntentResultDto> CreatePaymentIntentAsync(
        decimal amount, string currency, string? description)
    {
        var options = new PaymentIntentCreateOptions
        {
            Amount = (long)amount,
            Currency = currency.ToLowerInvariant(),
            Description = description,
            AutomaticPaymentMethods = new PaymentIntentAutomaticPaymentMethodsOptions
            {
                Enabled = true
            }
        };

        var intent = await _intentService.CreateAsync(options);

        _logger.Information(
            "[StripeService] Created PaymentIntent {IntentId} for {Amount} {Currency}",
            intent.Id, amount, currency);

        return new PaymentIntentResultDto
        {
            ClientSecret = intent.ClientSecret,
            PaymentIntentId = intent.Id
        };
    }

    /// <summary>Cancels a Stripe PaymentIntent that is still in a cancellable state.</summary>
    public async Task CancelPaymentIntentAsync(string paymentIntentId)
    {
        await _intentService.CancelAsync(paymentIntentId);
        _logger.Information("[StripeService] Canceled PaymentIntent {IntentId}", paymentIntentId);
    }

    /// <summary>Issues a full refund for the charge associated with the PaymentIntent.</summary>
    public async Task<RefundResultDto> RefundPaymentAsync(string paymentIntentId)
    {
        var intent = await _intentService.GetAsync(paymentIntentId);

        var options = new RefundCreateOptions
        {
            PaymentIntent = paymentIntentId
        };

        var refund = await _refundService.CreateAsync(options);

        _logger.Information(
            "[StripeService] Refund {RefundId} created for PaymentIntent {IntentId}",
            refund.Id, paymentIntentId);

        return new RefundResultDto
        {
            RefundId = refund.Id,
            Amount = refund.Amount,
            Status = refund.Status
        };
    }
}
