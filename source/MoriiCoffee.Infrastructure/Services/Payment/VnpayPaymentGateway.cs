using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Settings;

namespace MoriiCoffee.Infrastructure.Services.Payment;

public sealed class VnpayPaymentGateway : IPaymentGateway
{
    private readonly VnpaySettings _settings;
    private readonly VnpaySignatureService _signature;
    private readonly VnpayClock _clock;
    private readonly HttpClient _httpClient;

    public VnpayPaymentGateway(
        VnpaySettings settings,
        VnpaySignatureService signature,
        VnpayClock clock,
        HttpClient httpClient)
    {
        _settings = settings;
        _signature = signature;
        _clock = clock;
        _httpClient = httpClient;
    }

    public EPaymentProvider Provider => EPaymentProvider.Vnpay;
    public string PublishableKey => string.Empty;

    public Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var txnRef = request.ClientReferenceId ?? Guid.NewGuid().ToString("N");
        var createDate = _clock.FormatNow();
        var expireDate = _clock.Format(_clock.UtcNow.AddMinutes(_settings.PaymentExpiryMinutes));
        var values = new Dictionary<string, string?>
        {
            ["vnp_Version"] = _settings.Version,
            ["vnp_Command"] = "pay",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_Amount"] = checked(request.TotalAmount * 100).ToString(),
            ["vnp_CreateDate"] = createDate,
            ["vnp_CurrCode"] = _settings.Currency,
            ["vnp_IpAddr"] = request.Metadata.GetValueOrDefault("ipAddress", "127.0.0.1"),
            ["vnp_Locale"] = _settings.Locale,
            ["vnp_OrderInfo"] = $"Thanh toan don hang {txnRef}",
            ["vnp_OrderType"] = _settings.OrderType,
            ["vnp_ReturnUrl"] = _settings.ReturnUrl,
            ["vnp_TxnRef"] = txnRef,
            ["vnp_ExpireDate"] = expireDate
        };
        var canonical = _signature.Canonicalize(values);
        var hash = _signature.Sign(values, _settings.HashSecret);
        return Task.FromResult(new CheckoutSessionResult
        {
            SessionId = txnRef,
            Url = $"{_settings.PaymentUrl}?{canonical}&vnp_SecureHash={hash}",
            ExpiresAtUtc = _clock.UtcNow.AddMinutes(_settings.PaymentExpiryMinutes)
        });
    }

    public async Task<CheckoutSessionStatusResult> GetCheckoutSessionStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default) =>
        await GetCheckoutSessionStatusAsync(sessionId, _clock.UtcNow, cancellationToken);

    public async Task<CheckoutSessionStatusResult> GetCheckoutSessionStatusAsync(
        string sessionId,
        DateTime transactionDateUtc,
        CancellationToken cancellationToken = default)
    {
        EnsureConfigured();
        var requestId = Guid.NewGuid().ToString("N");
        var createDate = _clock.FormatNow();
        var transactionDate = _clock.Format(transactionDateUtc);
        var ipAddress = "127.0.0.1";
        var orderInfo = $"Query transaction {sessionId}";
        var raw = string.Join('|', requestId, _settings.Version, "querydr", _settings.TmnCode,
            sessionId, transactionDate, createDate, ipAddress, orderInfo);
        var request = new Dictionary<string, string?>
        {
            ["vnp_RequestId"] = requestId,
            ["vnp_Version"] = _settings.Version,
            ["vnp_Command"] = "querydr",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_TxnRef"] = sessionId,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_TransactionDate"] = transactionDate,
            ["vnp_CreateDate"] = createDate,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_SecureHash"] = _signature.SignRaw(raw, _settings.HashSecret)
        };

        using var response = await _httpClient.PostAsJsonAsync(_settings.ApiUrl, request, cancellationToken);
        response.EnsureSuccessStatusCode();
        var values = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(cancellationToken)
            ?? throw new InvalidOperationException("VNPAY QueryDR returned an empty response.");
        VerifySignedResponse(values, "QueryDR");
        var responseCode = values.GetValueOrDefault("vnp_ResponseCode").ToString();
        var transactionStatus = values.GetValueOrDefault("vnp_TransactionStatus").ToString();
        var transactionNo = values.GetValueOrDefault("vnp_TransactionNo").ToString();
        var state = responseCode == "00" && transactionStatus == "00"
            ? ECheckoutSessionState.Paid
            : transactionStatus is "01" or "02" ? ECheckoutSessionState.Pending : ECheckoutSessionState.Expired;

        return new CheckoutSessionStatusResult
        {
            SessionId = sessionId,
            State = state,
            PaymentIntentId = transactionNo,
            ChargeId = transactionNo,
            FailureReason = state == ECheckoutSessionState.Expired ? $"VNPAY status {responseCode}/{transactionStatus}" : null
        };
    }

    public WebhookEventEnvelope ConstructWebhookEvent(string rawBody, string? signatureHeader)
    {
        EnsureConfigured();
        var values = ParseQuery(rawBody);
        var suppliedHash = values.GetValueOrDefault("vnp_SecureHash") ?? string.Empty;
        if (!_signature.Verify(values, suppliedHash, _settings.HashSecret))
            throw new PaymentGatewaySignatureException("VNPAY callback signature verification failed.");
        if (!string.Equals(values.GetValueOrDefault("vnp_TmnCode"), _settings.TmnCode, StringComparison.Ordinal))
            throw new PaymentGatewaySignatureException("VNPAY callback terminal code does not match this merchant.");

        var txnRef = values.GetValueOrDefault("vnp_TxnRef") ?? string.Empty;
        var transactionNo = values.GetValueOrDefault("vnp_TransactionNo") ?? "0";
        var responseCode = values.GetValueOrDefault("vnp_ResponseCode") ?? "99";
        var transactionStatus = values.GetValueOrDefault("vnp_TransactionStatus") ?? responseCode;
        var succeeded = responseCode == "00" && transactionStatus == "00";
        var amount = long.TryParse(values.GetValueOrDefault("vnp_Amount"), out var scaled) && scaled % 100 == 0
            ? scaled / 100
            : -1;

        return new WebhookEventEnvelope
        {
            Provider = EPaymentProvider.Vnpay,
            EventId = $"VNPAY:{txnRef}:{transactionNo}:{responseCode}:{transactionStatus}",
            EventType = succeeded ? "checkout.session.completed" : "payment_intent.payment_failed",
            EventKind = succeeded ? EPaymentProviderEventKind.PaymentSucceeded : EPaymentProviderEventKind.PaymentFailed,
            RawBody = rawBody,
            ProviderSessionId = txnRef,
            ProviderPaymentId = transactionNo,
            ProviderChargeId = transactionNo,
            MetadataCheckoutDraftId = Guid.TryParse(txnRef, out var draftId) ? draftId : null,
            Amount = amount,
            FailureReason = succeeded ? null : $"VNPAY response {responseCode}/{transactionStatus}"
        };
    }

    public async Task<RefundResult> CreateRefundAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        if (!_settings.RefundEnabled)
            throw new NotSupportedException("VNPAY refunds are disabled by configuration.");
        EnsureConfigured();
        ArgumentException.ThrowIfNullOrWhiteSpace(request.ProviderSessionId);
        var requestId = Guid.NewGuid().ToString("N");
        var createDate = _clock.FormatNow();
        var transactionDate = createDate;
        var transactionType = request.IsFullRefund ? "02" : "03";
        var orderInfo = request.Reason ?? $"Refund order {request.OrderId}";
        var ipAddress = "127.0.0.1";
        var amount = checked(request.Amount * 100).ToString();
        var raw = string.Join('|', requestId, _settings.Version, "refund", _settings.TmnCode,
            transactionType, request.ProviderSessionId, amount, request.PaymentIntentId,
            transactionDate, request.InitiatedByAdminUserId, createDate, ipAddress, orderInfo);
        var payload = new Dictionary<string, string?>
        {
            ["vnp_RequestId"] = requestId,
            ["vnp_Version"] = _settings.Version,
            ["vnp_Command"] = "refund",
            ["vnp_TmnCode"] = _settings.TmnCode,
            ["vnp_TransactionType"] = transactionType,
            ["vnp_TxnRef"] = request.ProviderSessionId,
            ["vnp_Amount"] = amount,
            ["vnp_TransactionNo"] = request.PaymentIntentId,
            ["vnp_TransactionDate"] = transactionDate,
            ["vnp_CreateBy"] = request.InitiatedByAdminUserId.ToString(),
            ["vnp_CreateDate"] = createDate,
            ["vnp_IpAddr"] = ipAddress,
            ["vnp_OrderInfo"] = orderInfo,
            ["vnp_SecureHash"] = _signature.SignRaw(raw, _settings.HashSecret)
        };
        using var response = await _httpClient.PostAsJsonAsync(_settings.ApiUrl, payload, cancellationToken);
        response.EnsureSuccessStatusCode();
        var values = await response.Content.ReadFromJsonAsync<Dictionary<string, JsonElement>>(cancellationToken)
            ?? throw new InvalidOperationException("VNPAY refund returned an empty response.");
        VerifySignedResponse(values, "refund");
        var responseCode = values.GetValueOrDefault("vnp_ResponseCode").ToString();
        return new RefundResult
        {
            RefundId = values.GetValueOrDefault("vnp_TransactionNo").ToString() is { Length: > 0 } id ? id : requestId,
            Status = responseCode == "00" ? "succeeded" : "failed"
        };
    }

    public Task<PaymentProviderStatusResult> GetPaymentStatusAsync(
        string paymentIntentId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException("VNPAY refund reconciliation is unavailable until refunds are enabled.");

    private void EnsureConfigured()
    {
        if (string.IsNullOrWhiteSpace(_settings.TmnCode) || string.IsNullOrWhiteSpace(_settings.HashSecret))
            throw new InvalidOperationException("VNPAY is not configured.");
    }

    private void VerifySignedResponse(Dictionary<string, JsonElement> values, string operation)
    {
        if (!values.TryGetValue("vnp_SecureHash", out var responseHash))
            throw new PaymentGatewaySignatureException($"VNPAY {operation} response is missing its signature.");

        var signable = values.ToDictionary(
            pair => pair.Key,
            pair => (string?)pair.Value.ToString(),
            StringComparer.Ordinal);
        if (!_signature.Verify(signable, responseHash.ToString(), _settings.HashSecret))
            throw new PaymentGatewaySignatureException($"VNPAY {operation} response signature verification failed.");
    }

    private static Dictionary<string, string?> ParseQuery(string rawQuery)
    {
        return rawQuery.TrimStart('?')
            .Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .ToDictionary<string[], string, string?>(
                part => WebUtility.UrlDecode(part[0]),
                part => part.Length == 2 ? WebUtility.UrlDecode(part[1]) : string.Empty,
                StringComparer.Ordinal);
    }
}
