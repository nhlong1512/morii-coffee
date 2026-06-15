---
title: Frontend Handoff - Implementation Checklist
description: Complete checklist for VNPAY frontend implementation
nav_title: Frontend Checklist
---

# VNPAY Frontend Implementation Checklist

**Handoff Date**: June 15, 2026  
**Backend Status**: ✅ COMPLETE & VERIFIED  
**Frontend Status**: 📋 READY FOR IMPLEMENTATION  

---

## Pre-Implementation Review

- [ ] **Read FRONTEND_HANDOFF.md** completely
- [ ] **Review API contracts** (5 endpoints documented)
- [ ] **Understand security boundaries** (no secrets, no signing)
- [ ] **Confirm TypeScript schema** for SessionStorage
- [ ] **Check i18n keys** table for all labels

---

## Phase 1: Type Definitions & i18n

### TypeScript Types (src/types/)

- [ ] Add `PaymentMethod` type: `"COD" | "STRIPE" | "VNPAY"`
- [ ] Add `PaymentProvider` enum: `{ STRIPE = "Stripe", VNPAY = "Vnpay" }`
- [ ] Add `PaymentStatus` enum: `{ NotRequired, Pending, Paid, Failed }`
- [ ] Add `PendingHostedCheckout` interface:
  ```typescript
  interface PendingHostedCheckout {
    provider: "VNPAY" | "STRIPE";
    checkoutDraftId: string;
    providerSessionId: string;
    expiresAtUtc: string;
  }
  ```
- [ ] Update API response types for VNPAY endpoints
- [ ] Update Order/Payment history types with `provider` field

### Internationalization (src/i18n/messages/)

- [ ] Add `payment.method.vnpay` = "VNPAY" (English)
- [ ] Add `payment.method.vnpay` = "VNPAY" (Tiếng Việt)
- [ ] Add all 10 i18n keys from FRONTEND_HANDOFF.md
  - `checkout.vnpay_pending_verification`
  - `checkout.vnpay_success`
  - `checkout.vnpay_failed`
  - `checkout.vnpay_invalid`
  - `checkout.vnpay_expired`
  - `refund.vnpay_unavailable`
  - `error.payment_url_failed`
  - `error.reconcile_timeout`

---

## Phase 2: Checkout Integration

### Payment Method Selector (src/components/checkout/payment-method-selector.tsx)

- [ ] Add VNPAY option to payment method selector
- [ ] Display VNPAY icon (Lucide or approved brand asset)
- [ ] Use i18n key `payment.method.vnpay` for label
- [ ] Treat VNPAY as **prepaid** (zero COD for shipping)

### Shipping Quote Request (src/services/shipping-service.ts)

- [ ] Widen shipping payment type to accept `"VNPAY"`
- [ ] Send zero COD amount for VNPAY (like Stripe)
- [ ] Update type: `paymentMethod: "COD" | "STRIPE" | "VNPAY"`

### Payment Service Methods (src/services/payment-service.ts)

- [ ] Add `createVnpayPaymentUrl(request)`
  - Endpoint: `POST /api/v1/payments/vnpay/payment-url`
  - Body: Same as Stripe checkout-session (delivery + shipping)
  - Return: `{ paymentUrl, checkoutDraftId, txnRef, amount, currency, expiresAtUtc }`

- [ ] Add `reconcileVnpayPayment(checkoutDraftId, txnRef)`
  - Endpoint: `POST /api/v1/payments/vnpay/reconcile`
  - Return: `{ orderId, orderNumber, paymentStatus, failureReason, expiresAtUtc }`

### Checkout Page Logic (src/app/checkout/page.tsx)

- [ ] When `paymentMethod === "VNPAY"`:
  1. Call `createVnpayPaymentUrl(cartAndShipping)` with error handling
  2. Store pending checkout in `sessionStorage` with key `morii.pendingHostedCheckout`
  3. **Keep cart intact** during payment
  4. Redirect to `response.paymentUrl` with `window.location.assign()`
  5. Show clear error message if URL creation fails (with retry button)

- [ ] SessionStorage schema (copy from FRONTEND_HANDOFF.md):
  ```typescript
  {
    provider: "VNPAY",
    checkoutDraftId: string,
    providerSessionId: string,  // txnRef
    expiresAtUtc: string
  }
  ```

---

## Phase 3: Return Page Implementation

### Return Page (src/app/checkout/vnpay/return/page.tsx)

- [ ] Extract URL query params: `status`, `txnRef`
- [ ] Load pending checkout from `sessionStorage`
- [ ] **Show pending verification screen** even if `status=success`
  - Display message: i18n key `checkout.vnpay_pending_verification`
  - Start polling reconcile endpoint immediately

### Return State Component (src/components/checkout/vnpay-return-state.tsx)

Display different states:

- [ ] **Pending**: "Verifying payment..." (with spinner)
  - Poll reconcile endpoint every 2-3 seconds
  - Max timeout: 5 minutes or `expiresAtUtc` reached
  - Show countdown to expiry

- [ ] **Success** (`paymentStatus === "Paid"`):
  - Display message: i18n key `checkout.vnpay_success`
  - Show order number from reconcile response
  - Clear cart from localStorage
  - Clear sessionStorage (`morii.pendingHostedCheckout`)
  - Link to finalized order

- [ ] **Failed** (`paymentStatus === "Failed"`):
  - Display message: i18n key `checkout.vnpay_failed`
  - Show retry button → return to checkout

- [ ] **Invalid** (bad signature, unknown txnRef):
  - Display message: i18n key `checkout.vnpay_invalid`
  - Show "Start over" button → return to checkout

- [ ] **Expired** (current time > `expiresAtUtc`):
  - Display message: i18n key `checkout.vnpay_expired`
  - Show "Place new order" button → return to checkout

- [ ] **Timeout** (polling > 5 minutes):
  - Display message: i18n key `error.reconcile_timeout`
  - Show "Check order status" button → navigate to orders page
  - Clear sessionStorage

### Polling Logic

```typescript
async function pollForPaymentStatus(
  checkoutDraftId: string,
  txnRef: string,
  expiresAtUtc: string
) {
  const pollInterval = 2000;  // 2 seconds
  const maxDuration = 5 * 60 * 1000;  // 5 minutes
  const startTime = Date.now();

  while (true) {
    // Check timeout
    if (Date.now() - startTime > maxDuration) {
      setStatus("timeout");
      break;
    }

    // Check expiry
    if (Date.now() > new Date(expiresAtUtc).getTime()) {
      setStatus("expired");
      break;
    }

    // Poll reconcile
    const response = await reconcileVnpayPayment(checkoutDraftId, txnRef);
    
    if (response.paymentStatus === "Paid") {
      // Success - clear cart
      clearCart();
      setStatus("success");
      setOrderId(response.orderId);
      break;
    } else if (response.paymentStatus === "Failed") {
      setStatus("failed");
      break;
    }
    
    // Keep polling
    await new Promise(resolve => setTimeout(resolve, pollInterval));
  }
}
```

---

## Phase 4: Payment History & Admin Displays

### Payment History Query (src/services/payment-service.ts)

- [ ] Add `getPaymentByOrderId(orderId)`
  - Endpoint: `GET /api/v1/payments/by-order/{orderId}`
  - Returns: `{ provider, providerSessionId, providerPaymentId, amount, status, ... }`

### Customer Order Detail Page (src/app/orders/[id]/page.tsx)

Display payment section:
- [ ] Show payment method: "VNPAY"
- [ ] Show provider: "VNPAY"
- [ ] Show payment status (from order)
- [ ] Show VNPAY transaction number (from `providerPaymentId`)
- [ ] Show bank code & card type (when available)
- [ ] Show failure reason (for failed payments)
- [ ] Link to refund history (if applicable)

### Admin Order Detail Page (src/app/admin/orders/[id]/page.tsx)

Display comprehensive payment info:
- [ ] Payment method: "VNPAY"
- [ ] Provider: "VNPAY"
- [ ] Status (Pending/Paid/Failed)
- [ ] VNPAY transaction reference (`providerSessionId`)
- [ ] VNPAY transaction number (`providerPaymentId`)
- [ ] Bank code (when available)
- [ ] Card type (when available)
- [ ] Payment amount & date
- [ ] Refund history section:
  - [ ] Show refund requests with provider refund ID
  - [ ] Show refund status (Pending/Succeeded/Failed)
  - [ ] Show failure details (if any)

---

## Phase 5: Refund Management

### Refund Request (src/services/payment-service.ts)

- [ ] Add `requestRefund(orderId, amount, reason)`
  - Endpoint: `POST /api/v1/payments/{orderId}/refund`
  - Provider-routed automatically by backend
  - Returns: `{ refundId, status, provider, ... }`

### Admin Refund UI (src/app/admin/orders/[id]/page.tsx)

- [ ] Show "Request Refund" button (if applicable)
- [ ] Form: amount, reason (textarea)
- [ ] Submit and show refund status
- [ ] Handle error: i18n key `refund.vnpay_unavailable` if merchant doesn't have refund enabled
- [ ] Show pending/succeeded/failed states

### Refund Reconciliation (src/services/payment-service.ts)

- [ ] Add `reconcileRefund(orderId)`
  - Endpoint: `POST /api/v1/payments/{orderId}/refund/reconcile`
  - Updates pending refund status based on provider query
  - Returns updated refund state

---

## Phase 6: Testing

### Unit Tests (src/__tests__/)

- [ ] Payment type definitions and enums
- [ ] i18n keys exist for all VNPAY labels
- [ ] SessionStorage schema serialization/deserialization
- [ ] Polling logic (timeout, expiry, success/failure paths)

### Component Tests (src/__tests__/components/)

- [ ] Payment method selector renders VNPAY option
- [ ] VNPAY selected → treated as prepaid (zero COD)
- [ ] Checkout creates payment URL, stores sessionStorage, redirects
- [ ] URL creation failure shows error with retry button
- [ ] Return page loads pending checkout from sessionStorage
- [ ] Polling starts immediately (no user interaction needed)
- [ ] Cart **NOT cleared** until `paymentStatus === "Paid"`

### Integration Tests (src/__tests__/integration/)

- [ ] End-to-end checkout flow:
  1. User selects VNPAY
  2. Checkout creates payment URL
  3. Redirects to VNPAY sandbox page (mock)
  4. Return page shows pending state
  5. Polling completes when order paid
  6. Cart cleared, order displayed

- [ ] Edge cases:
  - User closes browser before return → sessionStorage persists
  - Polling timeout (5 min) → show "Check status" guidance
  - Payment expires → show "Expired" message with retry
  - Invalid return signature → show "Invalid transaction"
  - Concurrent reconcile calls → only one active poll

- [ ] Payment history displays VNPAY transaction details
- [ ] Admin can request/reconcile refunds when enabled
- [ ] Refund disabled → show appropriate message

### Manual Testing Checklist

- [ ] Checkout page shows VNPAY option (alongside COD/Stripe)
- [ ] Select VNPAY → payment URL created, browser redirects
- [ ] SessionStorage contains pending checkout data
- [ ] Return from VNPAY → pending verification screen
- [ ] Polling updates every 2-3 seconds
- [ ] Order appears after "Paid" status
- [ ] Cart cleared only after order created
- [ ] Order detail page shows VNPAY details
- [ ] Admin can view payment & refund history
- [ ] i18n labels in both English & Vietnamese

---

## Phase 7: Security Verification

- [ ] ✅ Frontend never imports/uses VNPAY SDK
- [ ] ✅ No HMAC signing in frontend code
- [ ] ✅ No HashSecret stored or passed to frontend
- [ ] ✅ Return endpoint response never marked as final (polling required)
- [ ] ✅ Cart persists until backend confirms paid state
- [ ] ✅ SessionStorage used (not localStorage) for sensitive checkout data
- [ ] ✅ Clear sessionStorage after success/failure
- [ ] ✅ No sensitive data exposed in logs or DevTools

---

## Known Constraints

- ⚠️ **Browser Return ≠ Authoritative**: Customer closing browser before return, or return page being bookmarked/refreshed, means frontend must poll reconcile endpoint. The `status` query param is **NOT** final proof.
- ⚠️ **Cart Persistence**: Cart must remain until backend confirms `Paid`. If customer navigates away during polling, cart should persist on return.
- ⚠️ **Payment Expiry**: If `expiresAtUtc` passes while polling, show "Payment expired" and require new checkout. Do not attempt to finalize after expiry.
- ⚠️ **VNPAY-Only MVP**: This implementation is VNPAY PAY only. Token, installment, and recurring payment products are excluded.

---

## API Summary (Quick Reference)

| Method | Endpoint | Auth | Purpose |
|--------|----------|------|---------|
| POST | `/api/v1/payments/vnpay/payment-url` | ✅ | Create signed payment URL |
| GET | `/api/v1/payments/vnpay/return` | ❌ | Browser return redirect (backend-initiated) |
| POST | `/api/v1/payments/vnpay/reconcile` | ✅ | Query payment status, finalize if paid |
| GET | `/api/v1/payments/by-order/{orderId}` | ✅ | Get payment history (provider-neutral) |
| POST | `/api/v1/payments/{orderId}/refund` | ✅ | Request refund (provider-routed) |
| POST | `/api/v1/payments/{orderId}/refund/reconcile` | ✅ | Reconcile refund status |

---

## Common Pitfalls to Avoid

❌ **Do NOT**:
- Treat browser `status=success` as final payment confirmation
- Clear cart before backend confirms order created
- Store HashSecret or signing logic in frontend
- Call VNPAY APIs directly from frontend
- Use localStorage for checkout data (use sessionStorage)
- Implement custom VNPAY signature verification

✅ **DO**:
- Poll reconcile endpoint until `Paid` or timeout
- Keep cart intact while payment pending
- Store only non-sensitive checkout metadata (ids, refs, times)
- Trust only backend `paymentStatus` response
- Handle polling timeout and expiry gracefully
- Clear sessionStorage after payment terminal state

---

## Sign-Off

**Backend**: ✅ COMPLETE  
**API Contracts**: ✅ VERIFIED  
**Documentation**: ✅ COMPREHENSIVE  
**Security Review**: ✅ PASSED  

**Status**: 🟢 **READY FOR FRONTEND IMPLEMENTATION**

Frontend team can start immediately. All required endpoints, data schemas, i18n keys, and flow diagrams are documented in [FRONTEND_HANDOFF.md](./FRONTEND_HANDOFF.md).

---

**Questions?** Refer to:
- [FRONTEND_HANDOFF.md](./FRONTEND_HANDOFF.md) — Detailed API & flow specs
- [INTEGRATION_VERIFICATION.md](./INTEGRATION_VERIFICATION.md) — Backend security/arch details
- [README.md](./README.md) — Complete implementation guide
- [QUICK_REFERENCE.md](./QUICK_REFERENCE.md) — File map & debugging

**Last Updated**: June 15, 2026  
**By**: Backend Team (VNPAY Integration Complete)
