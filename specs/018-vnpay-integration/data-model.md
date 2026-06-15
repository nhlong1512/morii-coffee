# Data Model: VNPAY Integration

## 1. Payment Provider

Identifies which external integration owns a payment attempt.

| Value | Meaning |
|---|---|
| `Stripe` | Existing Stripe-hosted payment |
| `Vnpay` | VNPAY PAY hosted payment |

Payment method remains the customer's checkout choice (`COD`, `STRIPE`, `VNPAY`, etc.). Provider is stored only for externally managed payment attempts.

## 2. Checkout Draft

Provider-neutral cached snapshot created before a prepaid order exists.

### Fields

| Field | Type | Notes |
|---|---|---|
| `draftId` | guid | Primary local correlation id |
| `userId` | guid | Required owner |
| `paymentMethod` | enum | `STRIPE` or `VNPAY` for hosted checkout |
| `provider` | enum | Required external provider |
| `providerSessionId` | string | Stripe session id or VNPAY `vnp_TxnRef` |
| `paymentStatus` | enum | Pending, Paid, Failed, Expired |
| `amount` | decimal | Authoritative VND amount |
| `currency` | string | `vnd` |
| `expiresAtUtc` | datetime | Required |
| `failureReason` | string? | Sanitized provider/local reason |
| delivery/shipping snapshot | existing fields | Reused from current checkout draft |
| items | collection | Authoritative cart snapshot |

### Validation Rules

- Provider session id is required before redirect.
- VNPAY transaction reference is unique and derived from the draft id.
- Draft amount is computed from cart plus validated shipping, never supplied by the frontend.
- Finalization uses the draft's payment method and provider.

## 3. Payment Attempt

Provider-neutral persisted payment transaction linked to a finalized order.

### Fields

| Field | Type | Notes |
|---|---|---|
| `id` | guid | Primary key |
| `orderId` | guid | Required FK |
| `provider` | enum | Required |
| `providerSessionId` | string | Required; VNPAY `vnp_TxnRef` |
| `providerPaymentId` | string? | VNPAY `vnp_TransactionNo` |
| `providerTransactionId` | string? | VNPAY bank transaction number or transaction number |
| `providerResponseCode` | string? | VNPAY response code |
| `providerTransactionStatus` | string? | Raw provider transaction status |
| `providerPayDateUtc` | datetime? | Parsed provider pay date |
| `providerBankCode` | string? | Optional support detail |
| `providerCardType` | string? | Optional support detail |
| `amount` | decimal | Authoritative VND amount |
| `currency` | string | Required ISO currency |
| `status` | enum | Created, Succeeded, Failed, Expired |
| `failureReason` | string? | Sanitized support detail |
| `refunds` | collection | Refund history |

### Indexes

- Unique: `(provider, providerSessionId)`
- Unique where populated: `(provider, providerPaymentId)`
- Non-unique: `orderId`

### State Transitions

| From | To | Trigger |
|---|---|---|
| `Created` | `Succeeded` | Verified authoritative success |
| `Created` | `Failed` | Verified terminal failure/fraud status |
| `Created` | `Expired` | Provider/local expiry |
| `Succeeded` | `Succeeded` | Duplicate success, same provider identity; no-op |
| `Succeeded` | any failure | Disallowed; record/audit without regressing paid state |

## 4. Order Payment Snapshot

Existing order payment state enriched with provider-neutral successful transaction identifiers.

### Fields

| Field | Type | Notes |
|---|---|---|
| `paymentMethod` | enum | Includes `VNPAY` |
| `paymentStatus` | enum | Existing order-level state |
| `paymentProvider` | enum? | Null for COD |
| `providerPaymentId` | string? | Successful provider payment identity |
| `providerTransactionId` | string? | Successful provider transaction identity |

### Validation Rules

- COD remains `NotRequired` and has no external provider.
- VNPAY starts pending and becomes paid only through verified IPN/QueryDR.
- Payment failure/reversal callbacks cannot regress an already-paid order automatically.

## 5. Payment Notification Audit

Provider-neutral idempotency and forensic record for Stripe webhooks and VNPAY IPNs.

### Fields

| Field | Type | Notes |
|---|---|---|
| `id` | guid | Primary key |
| `provider` | enum | Required |
| `providerEventId` | string | Required deterministic/provider identity |
| `eventKind` | enum | Normalized business event |
| `providerEventType` | string? | Raw provider event/status label |
| `payloadFingerprint` | string | SHA-256 fingerprint |
| `signatureVerified` | boolean | Required |
| `processingResult` | enum | Existing processing result model |
| `relatedPaymentId` | guid? | Optional FK |
| `errorMessage` | string? | Sanitized diagnostic |
| `receivedAt` | datetime | UTC |
| `processedAt` | datetime? | UTC |

### Indexes

- Unique: `(provider, providerEventId)`
- Descending: `receivedAt`

### VNPAY Event Identity

`VNPAY:{txnRef}:{transactionNo}:{responseCode}:{transactionStatus}`

## 6. Refund Record

Provider-neutral child of a successful payment attempt.

### Fields

| Field | Type | Notes |
|---|---|---|
| `id` | guid | Primary key |
| `paymentId` | guid | Required FK |
| `providerRefundId` | string | Required |
| `providerResponseCode` | string? | Optional |
| `providerTransactionStatus` | string? | Optional |
| `amount` | decimal | Required VND amount |
| `reason` | string? | Admin/support note |
| `status` | enum | Pending, Succeeded, Failed |
| `initiatedByAdminUserId` | guid | Required audit FK |

### Indexes

- Unique: `(payment.provider, providerRefundId)` or equivalent provider-owned uniqueness enforced with payment ownership.

### State Transitions

| From | To | Meaning |
|---|---|---|
| none | `Pending` | Provider accepted request or local request recorded |
| `Pending` | `Succeeded` | Verified settled refund |
| `Pending` | `Failed` | Verified rejected/failed refund |

## 7. Migration And Backfill

The provider-neutral migration must:

1. Rename Stripe-owned identifier columns to provider-neutral names without losing data.
2. Add provider and VNPAY diagnostic columns.
3. Backfill all existing payment attempts, order payment snapshots, refund records, and webhook audits with provider `Stripe`.
4. Replace Stripe-only unique indexes with provider-scoped indexes.
5. Preserve foreign keys, soft-delete behavior, and audit timestamps.
6. Verify rollback semantics and migration against a database containing existing Stripe rows.
