using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;

namespace MoriiCoffee.Application.Commands.Payment.CreatePaymentIntent;

/// <summary>
/// Calls Stripe to create a PaymentIntent, then persists a local Payment record in Pending state.
/// Returns the client secret so the frontend can confirm the payment with Stripe.js.
/// </summary>
public class CreatePaymentIntentCommandHandler
    : ICommandHandler<CreatePaymentIntentCommand, PaymentIntentResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;

    public CreatePaymentIntentCommandHandler(IUnitOfWork unitOfWork, IStripeService stripeService)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
    }

    public async Task<PaymentIntentResultDto> Handle(
        CreatePaymentIntentCommand request, CancellationToken cancellationToken)
    {
        var intentResult = await _stripeService.CreatePaymentIntentAsync(
            request.Amount, request.Currency, request.Description);

        var payment = new Domain.Aggregates.PaymentAggregate.Payment
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            Amount = request.Amount,
            Currency = request.Currency,
            StripePaymentIntentId = intentResult.PaymentIntentId,
            Description = request.Description
        };

        await _unitOfWork.Payments.CreateAsync(payment);
        await _unitOfWork.CommitAsync();

        intentResult.PaymentId = payment.Id;
        return intentResult;
    }
}
