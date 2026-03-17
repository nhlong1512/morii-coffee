using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.DTOs.Payment;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Domain.SeedWork.Command;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Payment;

namespace MoriiCoffee.Application.Commands.Payment.RefundPayment;

/// <summary>Refunds a succeeded payment via Stripe and marks it as Refunded in the database.</summary>
public class RefundPaymentCommandHandler : ICommandHandler<RefundPaymentCommand, RefundResultDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IStripeService _stripeService;

    public RefundPaymentCommandHandler(IUnitOfWork unitOfWork, IStripeService stripeService)
    {
        _unitOfWork = unitOfWork;
        _stripeService = stripeService;
    }

    public async Task<RefundResultDto> Handle(RefundPaymentCommand request, CancellationToken cancellationToken)
    {
        var payment = await _unitOfWork.Payments.GetByIdAsync(request.PaymentId)
            ?? throw new NotFoundException("Payment", request.PaymentId);

        if (payment.Status != EPaymentStatus.Succeeded)
            throw new BadRequestException($"Only succeeded payments can be refunded. Current status: '{payment.Status}'.");

        var refundResult = await _stripeService.RefundPaymentAsync(payment.StripePaymentIntentId);
        payment.MarkRefunded();

        await _unitOfWork.Payments.Update(payment);
        await _unitOfWork.CommitAsync();

        return refundResult;
    }
}
