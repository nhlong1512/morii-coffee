using FluentAssertions;
using Moq;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;
using MoriiCoffee.Infrastructure.Services.Payment;
using Xunit;

namespace MoriiCoffee.Application.Tests.Infrastructure.Payment;

public class VnpayPaymentGatewayTests
{
    [Fact]
    public async Task CreateCheckoutSession_ScalesVndExactlyOnceAndSignsUrl()
    {
        var gateway = CreateGateway(new DateTime(2026, 6, 15, 3, 0, 0, DateTimeKind.Utc));
        var result = await gateway.CreateCheckoutSessionAsync(new CreateCheckoutSessionRequest
        {
            ClientReferenceId = "draft-id",
            TotalAmount = 125000,
            Metadata = new Dictionary<string, string> { ["ipAddress"] = "127.0.0.1" }
        });

        result.Url.Should().Contain("vnp_Amount=12500000");
        result.Url.Should().Contain("vnp_SecureHash=");
    }

    [Fact]
    public void ConstructWebhookEvent_MapsVerifiedSuccess()
    {
        var settings = Settings();
        var signature = new VnpaySignatureService();
        var values = new Dictionary<string, string?>
        {
            ["vnp_TmnCode"] = settings.TmnCode,
            ["vnp_TxnRef"] = Guid.NewGuid().ToString(),
            ["vnp_TransactionNo"] = "123",
            ["vnp_ResponseCode"] = "00",
            ["vnp_TransactionStatus"] = "00",
            ["vnp_Amount"] = "12500000"
        };
        var query = signature.Canonicalize(values) + "&vnp_SecureHash=" + signature.Sign(values, settings.HashSecret);

        var envelope = CreateGateway(DateTime.UtcNow).ConstructWebhookEvent(query, null);

        envelope.Provider.Should().Be(EPaymentProvider.Vnpay);
        envelope.EventKind.Should().Be(EPaymentProviderEventKind.PaymentSucceeded);
        envelope.Amount.Should().Be(125000);
    }

    [Fact]
    public void Clock_FormatsUtcAsVietnamTime()
    {
        var provider = new Mock<IDateTimeProvider>();
        provider.SetupGet(x => x.UtcNow).Returns(new DateTime(2026, 6, 15, 3, 0, 0, DateTimeKind.Utc));

        new VnpayClock(provider.Object).FormatNow().Should().Be("20260615100000");
    }

    private static VnpayPaymentGateway CreateGateway(DateTime utcNow)
    {
        var clock = new Mock<IDateTimeProvider>();
        clock.SetupGet(x => x.UtcNow).Returns(utcNow);
        return new VnpayPaymentGateway(Settings(), new VnpaySignatureService(), new VnpayClock(clock.Object), new HttpClient());
    }

    private static VnpaySettings Settings() => new()
    {
        TmnCode = "TEST",
        HashSecret = "test-secret",
        ReturnUrl = "https://api.test/vnpay/return"
    };
}
