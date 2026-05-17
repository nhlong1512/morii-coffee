# Phase 1 — Data Model

**Feature**: 011-stripe-payment
**Date**: 2026-05-14

This document defines the new entities, the existing-entity changes, the schema migration, and the state machines they enforce.

---

## ER overview

```
┌──────────────────┐ 1     N ┌─────────────────────────────┐ 1     N ┌────────────────┐
│ Order (existing) │────────►│ Payment (new aggregate root)│────────►│ RefundRecord   │
│ + PaymentStatus  │         │                             │         │   (new child)  │
└──────────────────┘         └─────────────────────────────┘         └────────────────┘
                                            ▲ 1
                                            │
                                            │ (loose; matched by StripeEventId/SessionId
                                            │  in handler, no FK to keep audit independent)
                                            │
                              ┌─────────────────────────────┐
                              │ PaymentWebhookEvent (new)   │
                              │ audit / idempotency table   │
                              └─────────────────────────────┘
```

---

## 1. `Order` aggregate — extension

**File**: `source/MoriiCoffee.Domain/Aggregates/OrderAggregate/Order.cs` *(MODIFIED)*

### Added properties

| Property | Type | Constraints | Purpose |
|---|---|---|---|
| `PaymentStatus` | `EPaymentStatus` (new enum) | `[Required]`, `[JsonConverter(typeof(JsonStringEnumConverter))]` | Distinct from `OrderStatus`; tracks payment lifecycle. |

### Added factory rule

The existing `Order.Create(...)` factory is extended to set the initial `PaymentStatus`:

| `PaymentMethod` value | Initial `PaymentStatus` |
|---|---|
| `COD` | `NotRequired` |
| `STRIPE` (new) | `Pending` |
| `MOMO`, `PAYPAL` (unused in this feature) | `Pending` (defensive default; these methods are still on the roadmap and outside this feature) |

### Added methods

```csharp
public void MarkPaymentPaid(string stripePaymentIntentId, string stripeChargeId);
public void MarkPaymentFailed();
public void ApplyRefund(decimal cumulativeRefundedAmount);
```

**Invariants enforced inside `Order`** (each throws `InvalidOperationException` on violation):

- `MarkPaymentPaid` requires `PaymentStatus == Pending`. If called with the same `stripePaymentIntentId` after the first successful call, the method is a **no-op** (idempotent). If called with a *different* PI id, it throws.
- `MarkPaymentFailed` requires `PaymentStatus == Pending`.
- `ApplyRefund` requires `PaymentStatus ∈ {Paid, PartiallyRefunded}` and `cumulativeRefundedAmount ∈ (0, Total]`. Sets `PaymentStatus = Refunded` when `cumulativeRefundedAmount == Total`, otherwise `PartiallyRefunded`.
- `Confirm()` is extended to throw when `PaymentMethod == STRIPE` AND `PaymentStatus != Paid` (FR-013). COD path is unchanged.

### Migration impact

- Add `PaymentStatus` column to `Orders` table — `int NOT NULL DEFAULT 1` where `1 = NotRequired`. The default lets pre-existing rows (all COD) round-trip without breakage.

---

## 2. `Payment` — new aggregate root

**File**: `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Payment.cs` *(NEW)*

One row per Stripe Checkout Session created against an Order. Multiple `Payment` rows per `Order` are allowed (a failed first attempt is followed by a successful second attempt).

```
Payment
├── Id : Guid (PK)
├── OrderId : Guid (FK → Orders.Id, ON DELETE RESTRICT)
├── StripeSessionId : string (UNIQUE, length 200, e.g., "cs_test_...")
├── StripePaymentIntentId : string? (length 200, populated after Stripe creates a PI)
├── StripeChargeId : string? (length 200, populated when the charge succeeds)
├── Amount : decimal(18,2) (snapshot of order total in VND)
├── Currency : string (length 3, e.g., "vnd")
├── Status : EPaymentTransactionStatus (Created, Succeeded, Failed, Expired)
├── FailureReason : string? (length 500, populated on Failed)
├── CreatedAt : DateTime (UTC)
├── UpdatedAt : DateTime (UTC)
└── _refunds : List<RefundRecord> (private collection)
```

### Factory

```csharp
public static Payment Create(Guid orderId, string stripeSessionId, decimal amount, string currency);
```

### Methods

- `MarkSucceeded(string paymentIntentId, string chargeId)` — guards: status must be `Created`.
- `MarkFailed(string reason)` — guards: status must be `Created`.
- `MarkExpired()` — guards: status must be `Created`.
- `AddRefund(RefundRecord refund)` — guards: status must be `Succeeded`; the sum of all refund amounts must not exceed `Amount`.

### Indexes

| Index | Columns | Type | Why |
|---|---|---|---|
| `IX_Payments_OrderId` | `OrderId` | non-unique | List a customer's payment attempts for a given order. |
| `IX_Payments_StripeSessionId` | `StripeSessionId` | UNIQUE | Webhook handler looks up payment by session id. |
| `IX_Payments_StripePaymentIntentId` | `StripePaymentIntentId` | non-unique (nullable) | Refund handler lookup. |

---

## 3. `RefundRecord` — new child entity of `Payment`

**File**: `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Entities/RefundRecord.cs` *(NEW)*

```
RefundRecord
├── Id : Guid (PK)
├── PaymentId : Guid (FK → Payments.Id, ON DELETE CASCADE)
├── StripeRefundId : string (UNIQUE, length 200, e.g., "re_…")
├── Amount : decimal(18,2)
├── Reason : string? (length 500)
├── Status : ERefundStatus (Pending, Succeeded, Failed)
├── InitiatedByAdminUserId : Guid (FK → AspNetUsers.Id, ON DELETE RESTRICT)
├── CreatedAt : DateTime (UTC)
└── UpdatedAt : DateTime (UTC)
```

### Methods

- `MarkSucceeded()` — guards: status must be `Pending`.
- `MarkFailed(string? reason)` — guards: status must be `Pending`.

---

## 4. `PaymentWebhookEvent` — new audit / idempotency entity

**File**: `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Entities/PaymentWebhookEvent.cs` *(NEW)*

```
PaymentWebhookEvent
├── Id : Guid (PK)
├── StripeEventId : string (UNIQUE, length 200, e.g., "evt_…")
├── EventType : string (length 100, e.g., "checkout.session.completed")
├── PayloadFingerprint : string (length 64, SHA-256 hex of the raw body — for forensic compare without storing full payload)
├── SignatureVerified : bool
├── ProcessingResult : EPaymentWebhookProcessingResult (Processed, Duplicate, SignatureInvalid, OrderNotFound, Failed)
├── RelatedPaymentId : Guid? (FK → Payments.Id, ON DELETE SET NULL — soft link for diagnostics)
├── ErrorMessage : string? (length 1000, populated when ProcessingResult == Failed)
├── ReceivedAt : DateTime (UTC)
└── ProcessedAt : DateTime? (UTC)
```

This entity is **not** an aggregate root; it sits as a child of the Payment aggregate logically but is queried independently. The CASCADE rule deliberately uses SET NULL because the audit should outlive the Payment row.

### Indexes

| Index | Columns | Type | Why |
|---|---|---|---|
| `IX_PaymentWebhookEvents_StripeEventId` | `StripeEventId` | UNIQUE | **Primary idempotency gate** — INSERT race produces a constraint violation that the handler interprets as "duplicate". |
| `IX_PaymentWebhookEvents_ReceivedAt` | `ReceivedAt DESC` | non-unique | Operator: "what happened in the last hour". |

---

## 5. New enums

**File**: `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentMethod.cs` *(MODIFIED)*

```csharp
public enum EPaymentMethod
{
    COD = 1,
    MOMO = 2,
    PAYPAL = 3,
    STRIPE = 4   // NEW
}
```

**File**: `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentStatus.cs` *(NEW)*

```csharp
public enum EPaymentStatus
{
    NotRequired = 1,   // COD; no Stripe interaction
    Pending = 2,       // Stripe session created, no terminal event yet
    Paid = 3,
    Failed = 4,
    Refunded = 5,
    PartiallyRefunded = 6
}
```

**File**: `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentTransactionStatus.cs` *(NEW — per Payment row)*

```csharp
public enum EPaymentTransactionStatus
{
    Created = 1,    // Session created at Stripe, awaiting customer action
    Succeeded = 2,  // checkout.session.completed received and verified
    Failed = 3,     // payment_intent.payment_failed received
    Expired = 4     // checkout.session.expired received
}
```

**File**: `source/MoriiCoffee.Domain.Shared/Enums/Order/ERefundStatus.cs` *(NEW)*

```csharp
public enum ERefundStatus
{
    Pending = 1,    // Refund created at Stripe, awaiting charge.refunded webhook
    Succeeded = 2,
    Failed = 3
}
```

**File**: `source/MoriiCoffee.Domain.Shared/Enums/Order/EPaymentWebhookProcessingResult.cs` *(NEW)*

```csharp
public enum EPaymentWebhookProcessingResult
{
    Processed = 1,
    Duplicate = 2,
    SignatureInvalid = 3,
    OrderNotFound = 4,
    UnhandledEventType = 5,
    Failed = 6
}
```

---

## 6. Settings

**File**: `source/MoriiCoffee.Domain.Shared/Settings/StripeSettings.cs` *(NEW)*

```csharp
public class StripeSettings
{
    public string SecretKey { get; set; } = null!;
    public string PublishableKey { get; set; } = null!;
    public string WebhookSigningSecret { get; set; } = null!;
    public string Currency { get; set; } = "vnd";
    public string SuccessUrlTemplate { get; set; } = "/checkout/success?session_id={CHECKOUT_SESSION_ID}";
    public string CancelUrlPath { get; set; } = "/checkout/cancel";

    // Derived at startup — see ConfigureStripeOptions
    public bool IsLiveMode => SecretKey?.StartsWith("sk_live_", StringComparison.Ordinal) == true;
}
```

Bound to `Stripe` section. Validated at startup (`ValidateDataAnnotations` + custom check that `SecretKey` and `WebhookSigningSecret` are non-empty when not in design-time mode).

---

## 7. State machines (visual reference)

### `Order.PaymentStatus`

```
                    ┌─────────────┐
                    │ NotRequired │  (COD — terminal)
                    └─────────────┘

   ┌──────────┐  MarkPaymentPaid     ┌──────┐  ApplyRefund(partial)   ┌─────────────────────┐
   │ Pending  │ ───────────────────► │ Paid │ ──────────────────────► │ PartiallyRefunded   │
   └──────────┘                      └──┬───┘                         └──────┬──────────────┘
        │                               │                                    │
        │ MarkPaymentFailed             │ ApplyRefund(full)                  │ ApplyRefund(rest)
        ▼                               ▼                                    ▼
   ┌────────┐                      ┌──────────┐                         ┌──────────┐
   │ Failed │                      │ Refunded │ ◄───────────────────────│ Refunded │
   └────────┘                      └──────────┘                         └──────────┘
```

### `Payment.Status`

```
   ┌─────────┐  MarkSucceeded   ┌───────────┐
   │ Created │ ────────────────►│ Succeeded │
   └────┬────┘                  └───────────┘
        │
        │ MarkFailed                 (refunds attach to Succeeded)
        ▼
   ┌────────┐
   │ Failed │
   └────────┘
        ▲
        │ MarkExpired
   ┌─────────┐
   │ Expired │
   └─────────┘
```

### `RefundRecord.Status`

```
   ┌─────────┐  MarkSucceeded   ┌───────────┐
   │ Pending │ ────────────────►│ Succeeded │
   └────┬────┘                  └───────────┘
        │
        │ MarkFailed
        ▼
   ┌────────┐
   │ Failed │
   └────────┘
```

---

## 8. Repository interfaces

**File**: `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Repositories/IPaymentRepository.cs`

```csharp
public interface IPaymentRepository : IBaseRepository<Payment>
{
    Task<Payment?> GetBySessionIdAsync(string stripeSessionId, CancellationToken ct = default);
    Task<Payment?> GetByPaymentIntentIdAsync(string paymentIntentId, CancellationToken ct = default);
    Task<Payment?> GetLatestPendingByOrderIdAsync(Guid orderId, CancellationToken ct = default);
}
```

**File**: `source/MoriiCoffee.Domain/Aggregates/PaymentAggregate/Repositories/IPaymentWebhookEventRepository.cs`

```csharp
public interface IPaymentWebhookEventRepository
{
    /// <summary>Attempts to insert. Returns true if inserted (first time), false if a row with the same StripeEventId already exists (duplicate).</summary>
    Task<bool> TryInsertAsync(PaymentWebhookEvent evt, CancellationToken ct = default);

    Task UpdateAsync(PaymentWebhookEvent evt, CancellationToken ct = default);
}
```

Both registered through `IUnitOfWork` (new properties `Payments` and `PaymentWebhookEvents`).

---

## 9. Migration

**File**: `source/MoriiCoffee.Infrastructure.Persistence/Migrations/<timestamp>_AddStripePaymentSupport.cs`

Generated with:

```bash
dotnet ef migrations add AddStripePaymentSupport \
  --project source/MoriiCoffee.Infrastructure.Persistence/MoriiCoffee.Infrastructure.Persistence.csproj \
  --startup-project source/MoriiCoffee.Presentation/MoriiCoffee.Presentation.csproj \
  --output-dir Migrations
```

The migration must:

1. Add column `Orders.PaymentStatus int NOT NULL DEFAULT 1` (= `NotRequired`).
2. Create table `Payments` with all columns above and the three indexes.
3. Create table `Refunds` with FK → Payments and FK → AspNetUsers (admin).
4. Create table `PaymentWebhookEvents` with UNIQUE constraint on `StripeEventId`.

No seed data changes — payments are runtime-only.

---

## 10. Validation rules applied by request DTOs / handlers

| Rule | Where enforced |
|---|---|
| `OrderId` exists and belongs to the calling user | `CreateCheckoutSessionCommandValidator` + handler reads `_unitOfWork.Orders.GetByIdAsync` |
| `Order.PaymentMethod == STRIPE` | `CreateCheckoutSessionCommandHandler` (else throws `BadRequest`) |
| `Order.PaymentStatus == Pending` | same handler |
| Refund `amount <= Payment.Amount − Σ existing refunds` | `RefundPaymentCommandHandler` |
| Caller is admin | `[Authorize(Roles = nameof(ERole.ADMIN))]` on controller action |
| Webhook signature is valid | `HandleWebhookEventCommandHandler` via `IPaymentGateway.VerifyWebhookSignature(rawBody, header)` |
| Webhook is not a duplicate | `IPaymentWebhookEventRepository.TryInsertAsync` UNIQUE constraint |

---

## 11. What is **not** changed

- Existing `OrderItem`, `DeliveryInfo` — untouched.
- `UserDeliveryProfile`, `Cart`, `Product` — untouched.
- `PlaceOrderCommandHandler` is **modified** only to set `PaymentStatus = Pending` for `STRIPE` orders and `NotRequired` otherwise (already encapsulated by the `Order.Create` factory rule).
- No changes to authentication or authorisation infrastructure.
