using Microsoft.Extensions.Logging;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;
// Alias avoids a name collision between this Application.Commands.Payment namespace and the
// Domain Payment aggregate root type.
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Commands.Payment.CreateCheckoutSession;

/// <summary>
/// Creates a Stripe Checkout Session for the customer's order. Steps:
/// <list type="number">
/// <item>Load order, assert ownership + <c>PaymentMethod == STRIPE</c> + <c>PaymentStatus == Pending</c>.</item>
/// <item>Build line items + the Stripe-required success/cancel URLs.</item>
/// <item>Call <see cref="IPaymentGateway.CreateCheckoutSessionAsync"/>.</item>
/// <item>Persist a <see cref="Payment"/> row in <c>Created</c> status with the returned session id.</item>
/// </list>
/// Wrapped in a transaction so a failed Stripe call leaves no orphan rows.
/// </summary>
public class CreateCheckoutSessionCommandHandler
    : ICommandHandler<CreateCheckoutSessionCommand, CheckoutSessionResponseDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IPaymentGateway _gateway;
    private readonly StripeSettings _stripeSettings;
    private readonly EmailSettings _emailSettings;
    private readonly ILogger<CreateCheckoutSessionCommandHandler> _logger;

    public CreateCheckoutSessionCommandHandler(
        IUnitOfWork unitOfWork,
        IPaymentGateway gateway,
        StripeSettings stripeSettings,
        EmailSettings emailSettings,
        ILogger<CreateCheckoutSessionCommandHandler> logger)
    {
        _unitOfWork = unitOfWork;
        _gateway = gateway;
        _stripeSettings = stripeSettings;
        _emailSettings = emailSettings;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<CheckoutSessionResponseDto> Handle(
        CreateCheckoutSessionCommand command,
        CancellationToken cancellationToken)
    {
        // 1. Load order and verify caller is owner.
        var order = await _unitOfWork.Orders.GetByIdWithItemsAsync(command.OrderId);
        if (order is null)
            throw new NotFoundException("Order", command.OrderId);

        if (order.UserId != command.UserId)
            throw new UnauthorizedException("You are not authorized to pay for this order.");

        // 2. Pre-conditions: must be STRIPE + Pending + not cancelled.
        if (order.PaymentMethod != EPaymentMethod.STRIPE)
            throw new BadRequestException(
                $"This endpoint only supports Stripe payment orders (current method: {order.PaymentMethod}). " +
                "Orders with other payment methods (COD, MOMO, VNPAY) must use their respective endpoints.");

        if (order.PaymentStatus != EPaymentStatus.Pending)
            throw new BadRequestException(
                $"Order is not awaiting payment (current status: {order.PaymentStatus}).");

        if (order.OrderStatus == EOrderStatus.CANCELLED)
            throw new BadRequestException("Cannot pay for a cancelled order.");

        // 3. Build the gateway request. The internal Payment.Id is reserved here so we can echo
        //    it in Stripe metadata for the webhook to find us. The Payment row is persisted only
        //    after Stripe accepts the session, so a failed Stripe call leaves nothing behind.
        var reservedPaymentId = Guid.NewGuid();

        var storefrontUrl = _emailSettings.StorefrontUrl?.TrimEnd('/') ?? string.Empty;

        var request = new CreateCheckoutSessionRequest
        {
            OrderId = order.Id,
            PaymentId = reservedPaymentId,
            TotalAmount = (long)order.Total,
            Currency = _stripeSettings.Currency,
            Items = order.Items
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

        // 4. Call Stripe, then persist Payment row in a single DB transaction. If the Stripe call
        //    throws we never touch the DB, so there is no orphan row to clean up.
        var sessionResult = await _gateway.CreateCheckoutSessionAsync(request, cancellationToken);

        PaymentEntity paymentRow = null!;
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            paymentRow = PaymentEntity.Create(
                order.Id,
                sessionResult.SessionId,
                order.Total,
                _stripeSettings.Currency,
                reservedPaymentId);

            await _unitOfWork.Payments.CreateAsync(paymentRow);
        });

        _logger.LogInformation(
            "Created Stripe Checkout Session {SessionId} for Order {OrderId} (payment {PaymentId})",
            sessionResult.SessionId, order.Id, paymentRow.Id);

        return new CheckoutSessionResponseDto
        {
            SessionId = sessionResult.SessionId,
            CheckoutUrl = sessionResult.Url,
            ExpiresAtUtc = sessionResult.ExpiresAtUtc,
            PaymentId = paymentRow.Id,
            OrderId = order.Id,
            Amount = (long)order.Total,
            Currency = _stripeSettings.Currency,
            PublishableKey = _gateway.PublishableKey
        };
    }
}
