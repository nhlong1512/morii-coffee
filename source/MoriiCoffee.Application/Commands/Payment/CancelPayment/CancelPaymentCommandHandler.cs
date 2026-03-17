using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Payment;

namespace MoriiCoffee.Application.Commands.Payment.CancelPayment;

/// <summary>Cancels the Stripe PaymentIntent and updates the local record to Canceled.</summary>
public class CancelPaymentCommandHandler : ICommandHandler<CancelPaymentCommand, bool>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;

    public CancelPaymentCommandHandler(IUnitOfWork unitOfWork, IStripeService stripeService)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
    }

    public async Task<bool> Handle(CancelPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId)
            ?? throw new NotFoundException("Payment", request.PaymentId);

        if (payment.UserId != request.UserId)
            throw new UnauthorizedException("You do not have permission to cancel this payment.");

        if (payment.Status != EPaymentStatus.Pending)
            throw new BadRequestException($"Cannot cancel a payment with status '{payment.Status}'.");

        await _stripeService.CancelPaymentIntentAsync(payment.StripePaymentIntentId);
        payment.MarkCanceled();

        await _unitOfWork.Payments.Update(payment);
        await _unitOfWork.CommitAsync();

        return true;
    }
}
