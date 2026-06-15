# Provider-Neutral Payment History And Refunds

## Payment History

Existing route:

```http
GET /api/v1/payments/by-order/{orderId}
Authorization: Bearer {access-token}
```

Each payment attempt exposes provider-neutral fields:

```json
{
  "id": "...",
  "provider": "Vnpay",
  "providerSessionId": "d5c2672014e94ba8bd10cf737bb01a99",
  "providerPaymentId": "14123456",
  "providerTransactionId": "VNP123456",
  "providerResponseCode": "00",
  "providerTransactionStatus": "00",
  "providerPayDateUtc": "2026-06-15T10:20:00Z",
  "providerBankCode": "NCB",
  "providerCardType": "ATM",
  "amount": 125000,
  "currency": "vnd",
  "status": "Succeeded",
  "failureReason": null,
  "refunds": []
}
```

Existing Stripe rows are returned with `provider = "Stripe"` and their preserved identifiers.

## Refund

Existing admin route:

```http
POST /api/v1/payments/{orderId}/refund
Authorization: Bearer {admin-token}
```

The backend resolves the gateway from the persisted successful payment provider.

### Rules

- Full and partial refunds remain supported.
- VNPAY refund requests are rejected clearly when merchant capability is disabled.
- Accepted VNPAY refund requests begin as `Pending`.
- Provider refund status is verified before becoming `Succeeded` or `Failed`.
- Provider refund id is exposed as `providerRefundId`; Stripe-named response fields are removed.

## Refund Reconcile

Existing admin route:

```http
POST /api/v1/payments/{orderId}/refund/reconcile
Authorization: Bearer {admin-token}
```

Reconciliation routes by provider and never sends a VNPAY payment to Stripe or vice versa.
