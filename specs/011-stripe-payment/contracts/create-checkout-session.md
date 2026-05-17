# `POST /api/v1/payments/checkout-session`

**Auth**: Bearer JWT (any authenticated user). Caller must own the referenced order.

**Purpose**: Customer has already placed a Stripe-payment order (`PaymentMethod = STRIPE`, `PaymentStatus = Pending`) and now needs the redirect URL to the Stripe-hosted Checkout page.

> Note on flow ordering: the existing `PlaceOrder` endpoint stays unchanged. For Stripe orders the customer flow becomes:
> 1. `POST /api/v1/orders` (PlaceOrder) with `paymentMethod: "STRIPE"` → order created in `OrderStatus = PENDING, PaymentStatus = Pending`.
> 2. `POST /api/v1/payments/checkout-session` with the new `orderId` → returns Stripe URL.
> 3. Frontend redirects browser to that URL.

## Request

```http
POST /api/v1/payments/checkout-session HTTP/1.1
Authorization: Bearer <jwt>
Content-Type: application/json

{
  "orderId": "f3a7c2d1-..."
}
```

### Schema

```yaml
CreateCheckoutSessionDto:
  type: object
  required: [orderId]
  properties:
    orderId:
      type: string
      format: uuid
      description: Id of the Order to create a Checkout Session for.
```

## Response — 201 Created

```json
{
  "statusCode": 201,
  "message": "Created successfully",
  "data": {
    "sessionId": "cs_test_a1b2...",
    "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_test_a1b2...",
    "expiresAtUtc": "2026-05-15T07:30:00Z",
    "paymentId": "9b7e1a0c-...",
    "orderId": "f3a7c2d1-...",
    "amount": 137000,
    "currency": "vnd",
    "publishableKey": "pk_test_..."
  }
}
```

### Schema

```yaml
CheckoutSessionResponseDto:
  type: object
  properties:
    sessionId: { type: string, description: "Stripe Checkout Session id." }
    checkoutUrl: { type: string, format: uri, description: "Browser redirect target." }
    expiresAtUtc: { type: string, format: date-time }
    paymentId: { type: string, format: uuid, description: "Morii Coffee internal Payment row id." }
    orderId: { type: string, format: uuid }
    amount: { type: integer, description: "Amount in VND (no decimal multiplier)." }
    currency: { type: string, enum: ["vnd"] }
    publishableKey: { type: string, description: "Stripe publishable key — safe to expose. Returned for clients that want to render Stripe Elements later." }
```

## Errors

| HTTP | When | Body |
|---|---|---|
| 400 | Order is COD, or order's `PaymentStatus != Pending`, or order is cancelled. | `{ "statusCode": 400, "message": "Order is not awaiting online payment." }` |
| 401 | No JWT | standard middleware response |
| 404 | Order not found or not owned by caller. | `{ "statusCode": 404, "message": "Order not found." }` |
| 500 | Stripe API failed (network, key invalid, ...). | `{ "statusCode": 500, "message": "Unable to create payment session. Please try again." }` |

## Domain rules enforced

1. The order must exist and belong to the calling user (use `GetCurrentUserId()`).
2. `order.PaymentMethod == EPaymentMethod.STRIPE`.
3. `order.PaymentStatus == EPaymentStatus.Pending`.
4. `order.OrderStatus != EOrderStatus.CANCELLED`.
5. A new `Payment` row is persisted **before** the Stripe API is called? — No: we call Stripe first, then persist the returned `SessionId`, so a failed Stripe call leaves no orphan `Payment` row. The whole flow is wrapped in `IUnitOfWork.ExecuteInTransactionAsync`.

## Stripe SDK call

```csharp
var options = new SessionCreateOptions
{
    Mode = "payment",
    PaymentMethodTypes = new() { "card" },
    LineItems = order.Items.Select(item => new SessionLineItemOptions {
        PriceData = new() {
            Currency = settings.Currency,                          // "vnd"
            UnitAmount = (long)item.UnitPrice,                     // VND is zero-decimal
            ProductData = new() { Name = item.ProductName }
        },
        Quantity = item.Quantity
    }).ToList(),
    ClientReferenceId = order.Id.ToString(),
    Metadata = new() { ["orderId"] = order.Id.ToString(), ["paymentId"] = payment.Id.ToString() },
    SuccessUrl = $"{storefrontUrl}{settings.SuccessUrlTemplate}",
    CancelUrl  = $"{storefrontUrl}{settings.CancelUrlPath}"
};
var session = await stripeSessionService.CreateAsync(options, cancellationToken: ct);
```
