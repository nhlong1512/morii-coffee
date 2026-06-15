using FluentAssertions;
using MoriiCoffee.Infrastructure.Services.Payment;
using Xunit;

namespace MoriiCoffee.Application.Tests.Infrastructure.Payment;

public class VnpaySignatureServiceTests
{
    private readonly VnpaySignatureService _service = new();

    [Fact]
    public void Canonicalize_SortsEncodesAndExcludesSecureHash()
    {
        var result = _service.Canonicalize(new Dictionary<string, string?>
        {
            ["vnp_TxnRef"] = "draft 1",
            ["vnp_Amount"] = "100000",
            ["vnp_SecureHash"] = "secret"
        });

        result.Should().Be("vnp_Amount=100000&vnp_TxnRef=draft+1");
    }

    [Fact]
    public void Verify_RejectsTamperedValues()
    {
        var values = new Dictionary<string, string?> { ["vnp_Amount"] = "100000" };
        var hash = _service.Sign(values, "test-secret");
        values["vnp_Amount"] = "200000";

        _service.Verify(values, hash, "test-secret").Should().BeFalse();
    }
}
