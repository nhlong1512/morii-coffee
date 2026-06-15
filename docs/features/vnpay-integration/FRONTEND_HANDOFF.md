# VNPAY Frontend Handoff

## Security boundary

- The frontend must never receive `Vnpay__HashSecret`, build signatures, or call VNPAY transaction APIs directly.
- The browser return is display-only. It is not proof that payment succeeded.
- Clear purchased cart items only after backend payment/order state is `Paid`.

## Checkout

Add `VNPAY = 5` to the frontend payment-method model and treat it as prepaid. For GHN quotes, send a zero COD amount just like Stripe.

Call:

```http
POST /api/v1/payments/vnpay/payment-url
Authorization: Bearer {token}
```

The body is identical to the existing Stripe checkout-session delivery/shipping body. Do not send amount, IP address, transaction reference, or signed VNPAY fields.

Response `data`:

```json
{
  "checkoutDraftId": "uuid",
  "txnRef": "uuid",
  "paymentUrl": "https://sandbox.vnpayment.vn/...",
  "amount": 125000,
  "currency": "VND",
  "expiresAtUtc": "2026-06-15T10:30:00Z"
}
```

### Persistent Pending Checkout Storage

Persist checkout data in `sessionStorage` before redirecting to `paymentUrl`:

**Key**: `morii.pendingHostedCheckout`

**TypeScript Schema**:
```typescript
interface PendingHostedCheckout {
  provider: "VNPAY" | "STRIPE";
  checkoutDraftId: string;     // UUID
  providerSessionId: string;   // txnRef for VNPAY
  expiresAtUtc: string;        // ISO 8601 datetime
}
```

**Example**:
```typescript
sessionStorage.setItem("morii.pendingHostedCheckout", JSON.stringify({
  provider: "VNPAY",
  checkoutDraftId: "d5c26720-14e9-4ba8-bd10-cf737bb01a99",
  providerSessionId: "d5c2672014e94ba8bd10cf737bb01a99",
  expiresAtUtc: "2026-06-15T10:30:00Z"
}));
window.location.assign(paymentUrl);
```

Retry URL creation only after a clear API failure; never fabricate or modify the returned URL.

## Browser return

VNPAY returns through:

```http
GET /api/v1/payments/vnpay/return
```

The backend verifies the checksum and redirects to configured `Vnpay__StorefrontReturnUrl` with only:

- `status`: `success`, `failed`, or `invalid`
- `transactionReference`

Show a pending/verification screen even when `status=success`. Poll/reconcile until the backend reports an authoritative state.

## Reconciliation

Call:

```http
POST /api/v1/payments/vnpay/reconcile
Authorization: Bearer {token}
Content-Type: application/json

{ "checkoutDraftId": "uuid", "txnRef": "uuid" }
```

The checkout owner or an admin may reconcile. A successful verified QueryDR response finalizes the order idempotently.

### Polling Strategy

**Timing**: Immediately after return, then every 2-3 seconds  
**Max Duration**: 5 minutes (300 seconds) or until `expiresAtUtc` is reached  
**Stop Conditions**:
- `paymentStatus === "Paid"` → Success, proceed to order
- `paymentStatus === "Failed"` → User can retry checkout
- Current time > `expiresAtUtc` → Checkout expired, user must retry
- 300 seconds elapsed → Timeout, show retry option

### Response Schema

Response `data` includes:
```json
{
  "checkoutDraftId": "uuid",
  "txnRef": "uuid",
  "orderId": "uuid",        // null if not finalized
  "orderNumber": "MRC-...", // null if not finalized
  "paymentStatus": "Paid|Failed|NotRequired",
  "failureReason": "string or null",
  "expiresAtUtc": "2026-06-15T10:30:00Z"
}
```

## History and refunds

Existing payment history remains:

```http
GET /api/v1/payments/by-order/{orderId}
```

Payment rows now include `provider`; provider-neutral aliases include `providerSessionId` and `providerPaymentId`. Continue accepting legacy Stripe-named fields during the transition.

Admin refund routes remain provider-neutral:

```http
POST /api/v1/payments/{orderId}/refund
POST /api/v1/payments/{orderId}/refund/reconcile
```

When VNPAY merchant refund capability is disabled, show the backend rejection without changing optimistic local state.

## Internationalization (i18n)

Add translation keys for VNPAY checkout and return flows:

| Key | English | Tiếng Việt |
|-----|---------|-----------|
| `payment.method.vnpay` | VNPAY | VNPAY |
| `checkout.vnpay_pending_verification` | Verifying payment... | Đang xác nhận thanh toán... |
| `checkout.vnpay_success` | Payment successful. Your order is being processed. | Thanh toán thành công. Đơn hàng của bạn đang được xử lý. |
| `checkout.vnpay_failed` | Payment failed. Please try again. | Thanh toán thất bại. Vui lòng thử lại. |
| `checkout.vnpay_invalid` | Invalid transaction. Please start over. | Giao dịch không hợp lệ. Vui lòng bắt đầu lại. |
| `checkout.vnpay_expired` | Payment request expired. Please place a new order. | Yêu cầu thanh toán đã hết hạn. Vui lòng đặt hàng mới. |
| `refund.vnpay_unavailable` | Refunds are not available for this payment method at this time. | Hoàn tiền hiện không khả dụng cho phương thức thanh toán này. |
| `error.payment_url_failed` | Could not create payment URL. Please try again. | Không thể tạo liên kết thanh toán. Vui lòng thử lại. |
| `error.reconcile_timeout` | Payment verification timed out. Check your order status. | Xác nhận thanh toán hết thời gian. Kiểm tra trạng thái đơn hàng của bạn. |

---

## UI and tests

- Use the i18n keys above for checkout selection, pending verification, success/failed/invalid/expired states, and refund capability messaging.
- Test payment-method selection, prepaid shipping mapping, API failure retry, redirect persistence, invalid return, polling timeout, paid finalization, and no cart clearing before `Paid`.
- Test that frontend code contains no VNPAY secret or signing implementation.
- Test edge cases:
  - User closes browser before return → sessionStorage persists, polling resumes on return
  - Return with invalid signature → show "Invalid transaction" state
  - Polling timeout (5 minutes) → show "Check order status" guidance
  - Payment expires before polling completes → show "Payment request expired" state
  - Concurrent reconcile calls → deduplicate (only one active poll at a time)
