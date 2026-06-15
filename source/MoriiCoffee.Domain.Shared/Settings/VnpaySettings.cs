namespace MoriiCoffee.Domain.Shared.Settings;

public class VnpaySettings
{
    public string TmnCode { get; set; } = string.Empty;
    public string HashSecret { get; set; } = string.Empty;
    public string PaymentUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string ApiUrl { get; set; } = "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction";
    public string ReturnUrl { get; set; } = string.Empty;
    public string StorefrontReturnUrl { get; set; } = string.Empty;
    public string Currency { get; set; } = "VND";
    public string Locale { get; set; } = "vn";
    public string Version { get; set; } = "2.1.0";
    public string OrderType { get; set; } = "other";
    public int PaymentExpiryMinutes { get; set; } = 15;
    public bool RefundEnabled { get; set; }
}
