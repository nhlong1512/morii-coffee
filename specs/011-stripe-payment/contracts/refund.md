# `POST /api/v1/payments/{orderId}/refund`

**Auth**: Bearer JWT with role `ADMIN`. **Customers cannot call this.**

**Purpose**: Admin issues a full or partial refund against the latest successful payment on the given order.

## Request

```http
POST /api/v1/payments/f3a7c2d1-.../refund HTTP/1.1
Authorization: Bearer <admin-jwt>
Content-Type: application/json

{
  "amount": 50000,
  "reason": "Customer changed their mind on item X"
}
```

### Schema

```yaml
RefundDto:
  type: object
  properties:
    amount:
      type: integer
      nullable: true
      description: "Refund amount in VND. Null or omitted = full refund of the remaining unrefunded balance."
    reason:
      type: string
      nullable: true
      maxLength: 500
      description: "Free-text reason recorded against the refund."
```

## Response — 200 OK

```json
{
  "statusCode": 200,
  "message": "Refund initiated",
  "data": {
    "refundId": "5cb...",
    "stripeRefundId": "re_3OZB...",
    "amount": 50000,
    "status": "Pending",
    "paymentStatus": "PartiallyRefunded"
  }
}
```

> The order's `paymentStatus` shown in the response reflects the **optimistic** state we set immediately after Stripe accepts the refund. The authoritative confirmation arrives on the `charge.refunded` webhook seconds later and is reflected in subsequent queries.

### Schema

```yaml
RefundResponseDto:
  type: object
  properties:
    refundId: { type: string, format: uuid, description: "Internal RefundRecord id." }
    stripeRefundId: { type: string }
    amount: { type: integer }
    status: { type: string, enum: [Pending, Succeeded, Failed] }
    paymentStatus: { type: string, enum: [Paid, PartiallyRefunded, Refunded] }
```

## Errors

| HTTP | When | Body |
|---|---|---|
| 400 | `amount > Payment.Amount - sum(existing refunds)`, or order is not paid, or order has no successful payment. | `{ "statusCode": 400, "message": "Refund amount exceeds remaining unrefunded balance." }` |
| 401 | No JWT |
| 403 | JWT valid but not admin. | `{ "statusCode": 403, "message": "Forbidden" }` |
| 404 | Order not found. |
| 500 | Stripe API failed. We do NOT persist the local refund record in this case. |

## Domain rules enforced

1. Caller is admin (`[Authorize(Roles = nameof(ERole.ADMIN))]`).
2. Order exists.
3. There is at least one `Payment` with `Status == Succeeded` on the order.
4. `amount` ≤ that payment's `Amount - Σ(existing succeeded + pending refund amounts)`.
5. Refund is created at Stripe **first** using `RefundService.CreateAsync`. Only after Stripe accepts the refund (returning a `Refund` object) is the local `RefundRecord` persisted with `Status = Pending`. The `charge.refunded` webhook flips it to `Succeeded` and applies `Order.ApplyRefund`.

## Stripe SDK call

```csharp
var refund = await stripeRefundService.CreateAsync(new RefundCreateOptions
{
    PaymentIntent = payment.StripePaymentIntentId,
    Amount = (long)amount,
    Reason = string.IsNullOrWhiteSpace(reason) ? null : "requested_by_customer",
    Metadata = new() { ["adminUserId"] = adminUserId.ToString(), ["orderId"] = orderId.ToString(), ["adminReason"] = reason ?? "" }
}, cancellationToken: ct);
```
