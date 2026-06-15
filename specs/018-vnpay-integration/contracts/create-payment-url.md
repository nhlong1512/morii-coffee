# Create VNPAY Payment URL

## Request

```http
POST /api/v1/payments/vnpay/payment-url
Authorization: Bearer {access-token}
Content-Type: application/json
```

Request body reuses the existing hosted-checkout delivery and validated shipping-quote fields. It does not accept amount, transaction reference, signed fields, or customer IP.

## Success Response

```http
201 Created
```

```json
{
  "statusCode": 201,
  "message": "Created successfully",
  "data": {
    "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
    "checkoutDraftId": "d5c26720-14e9-4ba8-bd10-cf737bb01a99",
    "txnRef": "d5c2672014e94ba8bd10cf737bb01a99",
    "amount": 125000,
    "currency": "VND",
    "expiresAtUtc": "2026-06-15T10:30:00Z"
  }
}
```

## Errors

| HTTP | Meaning |
|---|---|
| `400` | Empty cart, invalid delivery/shipping quote, invalid calculated amount |
| `401` | Missing/invalid customer authentication |
| `404` | Required cart/product/shipping data not found |
| `500` | Draft persistence or provider URL generation failed; no redirect should occur |

## Invariants

- Draft is persisted before the URL is returned.
- `txnRef` is unique.
- `amount` is the authoritative backend-calculated VND total.
- URL expiry and returned expiry refer to the same checkout attempt.
