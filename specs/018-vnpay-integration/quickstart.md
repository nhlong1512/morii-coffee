# Quickstart: VNPAY Integration

## Goal

Verify the provider-neutral migration and VNPAY sandbox payment flow before implementation is considered complete.

## Preconditions

- VNPAY sandbox merchant terminal code and hash secret are available through backend secrets.
- A public HTTPS backend callback URL is configured in the VNPAY sandbox portal.
- Frontend/storefront return URL is configured.
- Provider-neutral database migration has been applied to a database containing representative Stripe history.
- An authenticated user has a valid cart and GHN shipping quote.

## Verification Steps

### 1. Build And Automated Checks

1. Run `rtk dotnet build MoriiCoffee.slnx`.
2. Run `rtk dotnet test MoriiCoffee.slnx`.
3. Confirm protocol-focused tests cover:
   - canonical query sorting and encoding
   - HMAC-SHA512 golden vectors and constant-time verification
   - VND multiplication/division by `100`
   - GMT+7 timestamp formatting
   - invalid checksum, terminal, reference, and amount
   - success only when both VNPAY success indicators equal `00`
   - QueryDR and refund response verification
4. Confirm application tests cover:
   - VNPAY checkout draft creation and stale shipping rejection
   - IPN finalization, duplicate delivery, amount mismatch, and paid-state preservation
   - owner/admin reconciliation authorization
   - provider-routed refund/reconcile behavior
   - provider-neutral payment history
5. Confirm existing Stripe and COD regression suites pass.

### 2. Migration Verification

1. Apply the provider-neutral migration to a database containing Stripe payment, webhook audit, order, and refund records.
2. Confirm existing records are backfilled as Stripe-owned.
3. Confirm provider-scoped unique indexes exist.
4. Confirm Stripe payment history, reconcile, and refund behavior still works.
5. Verify migration rollback in a disposable environment.

### 3. Create Payment URL

1. Authenticate as a customer with a populated cart and valid shipping quote.
2. Create a VNPAY payment URL.
3. Confirm the response contains a unique transaction reference, authoritative amount, expiry, and sandbox URL.
4. Confirm no signed secret or raw hash secret is exposed.
5. Attempt creation with an empty cart and stale shipping quote; confirm clear rejection.

### 4. Successful IPN And Return

1. Complete a VNPAY sandbox payment.
2. Confirm the verified IPN creates exactly one paid order/payment.
3. Replay the same IPN and confirm no duplicate order/payment transition.
4. Confirm the browser return redirects with sanitized values only.
5. Confirm the return request itself does not mutate payment state.

### 5. Failure And Integrity Cases

1. Submit callbacks with invalid checksum, wrong terminal, unknown reference, and wrong amount.
2. Confirm each creates no paid order and returns the required VNPAY outcome.
3. Test pending, failed, reversed, and suspected-fraud statuses.
4. Confirm an already-paid payment is not regressed.

### 6. Reconciliation

1. Delay or disable IPN delivery.
2. Return from VNPAY and call authenticated reconcile as the checkout owner.
3. Confirm verified QueryDR success finalizes exactly once.
4. Confirm pending/invalid QueryDR results do not incorrectly finalize payment.
5. Confirm another customer cannot reconcile the checkout.

### 7. Refunds

1. With merchant refund capability enabled, request full and partial refunds.
2. Confirm accepted requests remain pending until verified terminal status.
3. Reconcile refund state and confirm order payment status updates correctly.
4. With capability disabled, confirm a clear rejection and unchanged local state.

### 8. Frontend Handoff

1. Create `docs/features/vnpay-integration/FRONTEND_HANDOFF.md` after backend contracts are verified.
2. Confirm it documents checkout selection, redirect, pending storage, return polling, reconciliation, payment displays, security boundaries, and frontend tests.
3. Confirm it explicitly states that frontend never signs VNPAY data and never trusts browser return alone.

## Evidence Checklist

- Provider-neutral migration preserves existing Stripe history.
- Build succeeds with zero errors.
- All automated tests pass with zero failures.
- Successful VNPAY sandbox payment creates exactly one paid order.
- Duplicate, invalid, wrong-amount, and suspicious callbacks are safe.
- Return flow is read-only and sanitized.
- QueryDR reconciliation is verified and owner-authorized.
- Refund capability behavior is verified.
- Stripe and COD regressions pass.
- Frontend handoff document is complete.
