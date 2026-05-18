using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MoriiCoffee.Application.Commands.Payment.HandleWebhookEvent;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;
using PaymentEntity = MoriiCoffee.Domain.Aggregates.PaymentAggregate.Payment;

namespace MoriiCoffee.Application.Tests.Commands.Payment;

/// <summary>
/// US2 acceptance scenario 2.3: same notification delivered twice → state changed exactly once.
/// </summary>
public class HandleWebhookEventIdempotencyTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IOrderRepository> _ordersRepo = new();
    private readonly Mock<IPaymentRepository> _paymentsRepo = new();
    private readonly Mock<IPaymentWebhookEventRepository> _webhookRepo = new();
    private readonly Mock<IStripeCheckoutDraftService> _draftService = new();
    private readonly HandleWebhookEventCommandHandler _handler;

    public HandleWebhookEventIdempotencyTests()
    {
        _unitOfWork.Setup(u => u.Orders).Returns(_ordersRepo.Object);
        _unitOfWork.Setup(u => u.Payments).Returns(_paymentsRepo.Object);
        _unitOfWork.Setup(u => u.PaymentWebhookEvents).Returns(_webhookRepo.Object);
        _unitOfWork.Setup(u => u.CommitAsync()).ReturnsAsync(1);

        _handler = new HandleWebhookEventCommandHandler(
            _unitOfWork.Object,
            _draftService.Object,
            NullLogger<HandleWebhookEventCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_DuplicateEventId_ReturnsDuplicateAndDoesNotMutate()
    {
        // TryInsertAsync returns false → row already exists (UNIQUE constraint hit).
        _webhookRepo.Setup(r => r.TryInsertAsync(It.IsAny<PaymentWebhookEvent>())).ReturnsAsync(false);

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_duplicate",
                EventType = "checkout.session.completed",
                RawBody = "{}",
                ProviderSessionId = "cs_test_dup",
                ProviderPaymentId = "pi_dup"
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.Duplicate);

        // Critical: no Payment / Order mutations attempted on a duplicate.
        _paymentsRepo.Verify(r => r.GetBySessionIdAsync(It.IsAny<string>()), Times.Never);
        _ordersRepo.Verify(r => r.GetByIdAsync(It.IsAny<Guid>()), Times.Never);
    }

    [Fact]
    public async Task Handle_UnhandledEventType_ReturnsUnhandledAndDoesNotMutate()
    {
        _webhookRepo.Setup(r => r.TryInsertAsync(It.IsAny<PaymentWebhookEvent>())).ReturnsAsync(true);

        var command = new HandleWebhookEventCommand
        {
            Envelope = new WebhookEventEnvelope
            {
                EventId = "evt_unhandled",
                EventType = "customer.subscription.created", // not subscribed
                RawBody = "{}"
            }
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.UnhandledEventType);

        _paymentsRepo.Verify(r => r.GetBySessionIdAsync(It.IsAny<string>()), Times.Never);
        _paymentsRepo.Verify(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidSignature_AuditsAndReturnsSignatureInvalid()
    {
        _webhookRepo.Setup(r => r.TryInsertAsync(It.IsAny<PaymentWebhookEvent>())).ReturnsAsync(true);

        var result = await _handler.Handle(
            new HandleWebhookEventCommand
            {
                RawBody = "{\"id\":\"evt_forged\"}",
                SignatureVerified = false
            },
            CancellationToken.None);

        result.Result.Should().Be(EPaymentWebhookProcessingResult.SignatureInvalid);

        _webhookRepo.Verify(r => r.TryInsertAsync(
            It.Is<PaymentWebhookEvent>(evt =>
                !evt.SignatureVerified &&
                evt.EventType == "signature.invalid" &&
                evt.ProcessingResult == EPaymentWebhookProcessingResult.SignatureInvalid)),
            Times.Once);

        _paymentsRepo.Verify(r => r.GetBySessionIdAsync(It.IsAny<string>()), Times.Never);
        _paymentsRepo.Verify(r => r.GetByPaymentIntentIdAsync(It.IsAny<string>()), Times.Never);
    }
}
