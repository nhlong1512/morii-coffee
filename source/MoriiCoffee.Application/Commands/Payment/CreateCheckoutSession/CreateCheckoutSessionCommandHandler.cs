using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Cart;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;

/// <summary>
/// Creates a Stripe Checkout Session for the customer's current cart. Steps:
/// <list type="number">
/// <item>Load the customer's current cart and validate it is not empty.</item>
/// <item>Snapshot delivery info + cart items into a cached checkout draft.</item>
/// <item>Build line items + the Stripe-required success/cancel URLs.</item>
/// <item>Call <see cref="IPaymentGateway.CreateCheckoutSessionAsync"/>.</item>
/// <item>Persist the draft in cache so webhook/reconcile can finalize it into an order after payment succeeds.</item>
/// </list>
/// </summary>
public class CreateCheckoutSessionCommandHandler
    : ICommandHandler<CreateCheckoutSessionCommand, CheckoutSessionResponseDto>
{
    private readonly ICartService _cartService;
    private readonly IPaymentGateway _gateway;
    private readonly IStripeCheckoutDraftService _draftService;
    private readonly StripeSettings _stripeSettings;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<CreateCheckoutSessionCommandHandler> _logger;

    public CreateCheckoutSessionCommandHandler(
        ICartService cartService,
        IPaymentGateway gateway,
        IStripeCheckoutDraftService draftService,
        StripeSettings stripeSettings,
        EmailSettings emailSettings,
        ILogger<CreateCheckoutSessionCommandHandler> logger)
    {
        _cartService = cartService;
        _gateway = gateway;
        _draftService = draftService;
        _stripeSettings = stripeSettings;
        _emailSettings = emailSettings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CheckoutSessionResponseDto> Handle(
        CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken)
    {
        var cart = await _cartService.GetCartAsync(command.UserId);
        if (cart.Items.Count == 0)
            throw new BadRequestException("Cart is empty.");

        var storefrontUrl = _emailSettings.StorefrontUrl?.TrimEnd('/') ?? string.Empty;
        var draftId = Guid.NewGuid();
        var amount = cart.Items.Sum(item => item.UnitPrice * item.Quantity);

        var request = new CreateCheckoutSessionRequest
        {
            ClientReferenceId = draftId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["checkoutDraftId"] = draftId.ToString(),
                ["userId"] = command.UserId.ToString()
            },
            TotalAmount = (long)amount,
            Currency = _stripeSettings.Currency,
            Items = cart.Items
                .Select(i => new CheckoutLineItem
                {
                    Name = string.IsNullOrWhiteSpace(i.VariantLabel)
                        ? i.ProductName
                        : $"{i.ProductName} — {i.VariantLabel}",
                    UnitAmount = (long)i.UnitPrice,
                    Quantity = i.Quantity
                })
                .ToList(),
            SuccessUrl = $"{storefrontUrl}{_stripeSettings.SuccessUrlTemplate}",
            CancelUrl = $"{storefrontUrl}{_stripeSettings.CancelUrlPath}"
        };

        var sessionResult = await _gateway.CreateCheckoutSessionAsync(request, cancellationToken);
        await _draftService.StoreAsync(new StripeCheckoutDraftCacheDto
        {
            DraftId = draftId,
            UserId = command.UserId,
            FullName = command.FullName.Trim(),
            PhoneNumber = command.PhoneNumber.Trim(),
            Address = command.Address.Trim(),
            Notes = string.IsNullOrWhiteSpace(command.Notes) ? null : command.Notes.Trim(),
            SaveDeliveryProfile = command.SaveDeliveryProfile,
            Items = cart.Items.Select(CloneCartItem).ToList(),
            Amount = amount,
            Currency = _stripeSettings.Currency,
            SessionId = sessionResult.SessionId,
            ExpiresAtUtc = sessionResult.ExpiresAtUtc
        });

        _logger.LogInformation(
            "Created Stripe Checkout Session {SessionId} for draft {DraftId} and user {UserId}",
            sessionResult.SessionId, draftId, command.UserId);

        return new CheckoutSessionResponseDto
        {
            SessionId = sessionResult.SessionId,
            CheckoutUrl = sessionResult.Url,
            ExpiresAtUtc = sessionResult.ExpiresAtUtc,
            CheckoutDraftId = draftId,
            Amount = (long)amount,
            Currency = _stripeSettings.Currency,
            PublishableKey = _gateway.PublishableKey
        };
    }

    private static CartItemDto CloneCartItem(CartItemDto item)
    {
        return new CartItemDto
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
}
