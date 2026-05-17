# `GET /api/v1/payments/by-order/{orderId}`

**Auth**: Bearer JWT. Caller must be the order's owner OR an Admin.

**Purpose**: Inspect a payment + refund history for a given order. Used by the storefront's order-detail screen and the admin order-detail screen.

## Request

```http
GET /api/v1/payments/by-order/f3a7c2d1-... HTTP/1.1
Authorization: Bearer <jwt>
```

## Response — 200 OK

```json
{
  "statusCode": 200,
  "message": "Retrieved successfully",
  "data": {
    "orderId": "f3a7c2d1-...",
    "paymentStatus": "Paid",
    "payments": [
      {
        "id": "9b7e1a0c-...",
        "stripeSessionId": "cs_test_a1b2...",
        "stripePaymentIntentId": "pi_3OZA...",
        "amount": 137000,
        "currency": "vnd",
        "status": "Succeeded",
        "createdAt": "2026-05-14T03:21:00Z",
        "refunds": [
          {
            "id": "...",
            "stripeRefundId": "re_...",
            "amount": 50000,
            "reason": "Customer changed their mind on item X",
            "status": "Succeeded",
            "createdAt": "2026-05-14T05:00:00Z"
          }
        ]
      }
    ]
  }
}
```

### Schema

```yaml
OrderPaymentSummaryDto:
  type: object
  properties:
    orderId: { type: string, format: uuid }
    paymentStatus: { type: string, enum: [NotRequired, Pending, Paid, Failed, Refunded, PartiallyRefunded] }
    payments: { type: array, items: { $ref: '#/components/schemas/PaymentDto' } }

PaymentDto:
  type: object
  properties:
    id: { type: string, format: uuid }
    stripeSessionId: { type: string }
    stripePaymentIntentId: { type: string, nullable: true }
    amount: { type: integer }
    currency: { type: string }
    status: { type: string, enum: [Created, Succeeded, Failed, Expired] }
    failureReason: { type: string, nullable: true }
    createdAt: { type: string, format: date-time }
    refunds: { type: array, items: { $ref: '#/components/schemas/RefundDto' } }

RefundDto:
  type: object
  properties:
    id: { type: string, format: uuid }
    stripeRefundId: { type: string }
    amount: { type: integer }
    reason: { type: string, nullable: true }
    status: { type: string, enum: [Pending, Succeeded, Failed] }
    createdAt: { type: string, format: date-time }
```

## Errors

| HTTP | When |
|---|---|
| 401 | No JWT |
| 403 | JWT valid but caller is neither owner nor admin |
| 404 | Order does not exist |
