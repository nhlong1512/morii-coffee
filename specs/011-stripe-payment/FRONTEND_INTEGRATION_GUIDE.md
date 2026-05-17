# Stripe Payment Integration - Frontend Integration Guide

**Document Version**: 1.0  
**Last Updated**: 2026-05-18  
**Feature Branch**: 011-stripe-payment  
**Audience**: Frontend developers (React/Next.js)

---

## 📋 Table of Contents

1. [Quick Start](#-quick-start)
2. [Payment Methods & Enums](#-payment-methods--enums)
3. [API Endpoints](#-api-endpoints)
4. [Request/Response Contracts](#-requestresponse-contracts)
5. [State Machines](#-state-machines)
6. [Error Handling](#-error-handling)
7. [Implementation Checklist](#-implementation-checklist)
8. [Example Flows](#-example-flows)

---

## 🚀 Quick Start

### The Payment Flow (3 Steps)

```
1. Customer selects payment method (Stripe / COD) at checkout
                    ↓
2. If Stripe: Create checkout session → Redirect to Stripe-hosted page
                    ↓
3. After payment: Check order status → Show success/failure page
```

### Minimal Code Example

```typescript
// 1. User selects "Pay with Card" → call this
const response = await fetch("/api/v1/payments/stripe/checkout-session", {
  method: "POST",
  headers: { "Content-Type": "application/json" },
  body: JSON.stringify({ orderId: "abc-123" })
});

const { checkoutUrl } = await response.json();

// 2. Redirect to Stripe
window.location.href = checkoutUrl;

// 3. After redirect back, query order status
const order = await fetch("/api/v1/payments/by-order/abc-123").then(r => r.json());
console.log(order.paymentStatus); // "Paid" or "Failed" or "Pending"
```

---

## 🎯 Payment Methods & Enums

### **EPaymentMethod** (User selects at checkout)

```typescript
enum EPaymentMethod {
  COD = 1,          // Cash on Delivery
  MOMO = 2,         // MoMo e-wallet (future)
  PAYPAL = 3,       // PayPal (future)
  STRIPE = 4        // Online card payment ← MVP
}
```

**How to use**:
```typescript
// At checkout, show 2 options:
<input 
  type="radio" 
  value={EPaymentMethod.COD} 
  name="paymentMethod" 
/>
COD - Pay on Delivery

<input 
  type="radio" 
  value={EPaymentMethod.STRIPE} 
  name="paymentMethod" 
/>
Online Card (Stripe)
```

---

### **EPaymentStatus** (Order payment state)

```typescript
enum EPaymentStatus {
  NotRequired = 1,       // COD order → no payment needed
  Pending = 2,           // Stripe session created, waiting for payment
  Paid = 3,              // ✅ Payment confirmed
  Failed = 4,            // ❌ Payment failed → customer can retry
  Refunded = 5,          // 100% refunded (terminal)
  PartiallyRefunded = 6  // 0-99% refunded → more refunds possible
}
```

**State transitions**:
```
COD order:           NotRequired (terminal)
                     
Stripe happy path:   Pending → Paid → (PartiallyRefunded) → Refunded
                     
Stripe fail path:    Pending → Failed (customer retries or cancels)
```

**UI Logic**:
```typescript
function renderPaymentStatus(order: OrderDto) {
  switch (order.paymentStatus) {
    case EPaymentStatus.NotRequired:
      return <p>💵 Pay on Delivery</p>;
    case EPaymentStatus.Pending:
      return <p>⏳ Payment processing...</p>;
    case EPaymentStatus.Paid:
      return <p>✅ Payment confirmed</p>;
    case EPaymentStatus.Failed:
      return <p>❌ Payment failed - <button>Retry</button></p>;
    case EPaymentStatus.PartiallyRefunded:
      return <p>💰 Partially refunded (${order.refundedAmount})</p>;
    case EPaymentStatus.Refunded:
      return <p>↩️ Fully refunded</p>;
  }
}
```

---

### **EOrderStatus** (Fulfillment state - separate from payment)

```typescript
enum EOrderStatus {
  PENDING = 1,              // Order created, staff reviewing
  CONFIRMED = 2,            // ✅ Confirmed for fulfillment (only if Paid)
  READY_TO_PICKUP = 3,      // Ready at store
  IN_DELIVERY = 4,          // Out for delivery
  DELIVERED = 5,            // ✅ Complete
  REVIEWED = 6,             // Customer left review
  CANCELLED = 7             // Cancelled by customer or staff
}
```

**Important**: For Stripe orders, **CONFIRMED requires PaymentStatus = Paid**
```typescript
// This will fail:
// Order: { paymentStatus: Pending, orderStatus: PENDING }
// → Try to confirm() → HTTP 400 "Cannot confirm unpaid Stripe order"

// This will succeed:
// Order: { paymentStatus: Paid, orderStatus: PENDING }
// → confirm() → orderStatus = CONFIRMED ✅
```

---

## 🔌 API Endpoints

### **1. Create Checkout Session** (Redirect to Stripe)

```http
POST /api/v1/payments/stripe/checkout-session
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>

{
  "orderId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Response (201 Created)**:
```json
{
  "data": {
    "sessionId": "cs_test_1Mw0MN1EYL0...",
    "checkoutUrl": "https://checkout.stripe.com/pay/cs_test_...",
    "expiresAtUtc": "2026-05-19T14:30:00Z",
    "paymentId": "abc-def-ghi-123",
    "orderId": "550e8400-e29b-41d4-a716-446655440000",
    "amount": 250000,
    "currency": "vnd",
    "publishableKey": "pk_test_51TY7IQP0BXLeW8iC..."
  }
}
```

**Frontend Implementation**:
```typescript
const createCheckoutSession = async (orderId: string) => {
  const response = await fetch("/api/v1/payments/stripe/checkout-session", {
    method: "POST",
    headers: { 
      "Content-Type": "application/json",
      "Authorization": `Bearer ${getToken()}`
    },
    body: JSON.stringify({ orderId })
  });

  if (!response.ok) {
    const error = await response.json();
    throw new Error(error.message); // "Order not found" or "Payment method mismatch"
  }

  const { data } = await response.json();
  
  // Redirect to Stripe
  window.location.href = data.checkoutUrl;
  
  // Stripe will redirect back to:
  // - Success: https://your-domain/checkout/success?session_id=cs_test_...
  // - Cancel: https://your-domain/checkout/cancel
};
```

**Error Responses**:

| Status | Error | Meaning |
|--------|-------|---------|
| 400 | `Order does not exist` | Order ID not found |
| 400 | `Order is not configured for Stripe payment` | PaymentMethod ≠ STRIPE |
| 400 | `Order is not awaiting payment` | PaymentStatus ≠ Pending |
| 400 | `Cannot pay for a cancelled order` | Order.orderStatus = CANCELLED |
| 401 | `Unauthorized` | JWT token invalid/missing |
| 404 | `Not Found` | Order not found for user |

---

### **2. Get Payment Status** (After redirect back from Stripe)

```http
GET /api/v1/payments/by-order/{orderId}
Authorization: Bearer <JWT_TOKEN>
```

**Response (200 OK)**:
```json
{
  "data": {
    "orderId": "550e8400-e29b-41d4-a716-446655440000",
    "paymentStatus": "Paid",
    "orderStatus": "PENDING",
    "paymentMethod": "STRIPE",
    "totalAmount": 250000,
    "currency": "vnd",
    "payments": [
      {
        "id": "abc-def-ghi-123",
        "providerSessionId": "cs_test_1Mw0MN1EYL0...",
        "providerPaymentId": "pi_3OZA...",
        "amount": 250000,
        "status": "Succeeded",
        "createdAtUtc": "2026-05-18T14:00:00Z"
      }
    ],
    "refunds": [],
    "totalRefundedAmount": 0
  }
}
```

**Frontend Implementation**:
```typescript
const checkPaymentStatus = async (orderId: string) => {
  const response = await fetch(`/api/v1/payments/by-order/${orderId}`, {
    headers: { "Authorization": `Bearer ${getToken()}` }
  });

  if (!response.ok) throw new Error("Failed to fetch payment status");

  const { data } = await response.json();
  
  // Show appropriate UI based on paymentStatus
  if (data.paymentStatus === "Paid") {
    return <SuccessPage order={data} />;
  } else if (data.paymentStatus === "Failed") {
    return <FailurePage order={data} />;
  } else if (data.paymentStatus === "Pending") {
    return <PendingPage order={data} />;
  }
};
```

**When to call this?**
- ✅ When page loads after redirect from Stripe checkout
- ✅ When customer visits order detail page (to show current payment status)
- ✅ Polling (optional): every 5-10 seconds if Pending (in case webhook delayed)

---

### **3. Refund Payment** (Admin only)

```http
POST /api/v1/payments/{orderId}/refund
Content-Type: application/json
Authorization: Bearer <JWT_TOKEN>

{
  "amount": 250000,    // Optional: null/0 = full refund
  "reason": "Customer requested cancellation"
}
```

**Response (200 OK)**:
```json
{
  "data": {
    "refundId": "re_3OZB...",
    "status": "pending",
    "amount": 250000,
    "orderId": "550e8400-e29b-41d4-a716-446655440000",
    "createdAtUtc": "2026-05-18T14:15:00Z"
  }
}
```

**Frontend (Admin Dashboard)**:
```typescript
const refundOrder = async (orderId: string, amount?: number, reason?: string) => {
  const response = await fetch(`/api/v1/payments/${orderId}/refund`, {
    method: "POST",
    headers: { 
      "Content-Type": "application/json",
      "Authorization": `Bearer ${getToken()}`
    },
    body: JSON.stringify({ amount, reason })
  });

  if (!response.ok) {
    const error = await response.json();
    alert(`Refund failed: ${error.message}`);
    return;
  }

  const { data } = await response.json();
  alert(`Refund initiated. Status: ${data.status}`);
  // Refund is now "pending" → will become "succeeded" when webhook arrives
};
```

**Error Responses**:

| Status | Error | Meaning |
|--------|-------|---------|
| 400 | `Refund amount exceeds remaining balance` | amount > (total - alreadyRefunded) |
| 400 | `Order is not in a refundable state` | paymentStatus ∉ {Paid, PartiallyRefunded} |
| 403 | `Forbidden` | Only admin can refund |
| 404 | `Order not found` | Order ID doesn't exist |

---

## 📦 Request/Response Contracts

### **CreateCheckoutSessionRequest**

```typescript
interface CreateCheckoutSessionRequest {
  orderId: string; // UUID of the order to pay for
}
```

---

### **CheckoutSessionResponseDto**

```typescript
interface CheckoutSessionResponseDto {
  sessionId: string;                    // Stripe session id
  checkoutUrl: string;                  // HTTPS URL to redirect to
  expiresAtUtc: string;                 // ISO 8601 datetime
  paymentId: string;                    // Internal payment id
  orderId: string;
  amount: number;                       // In currency units (VND = đồng)
  currency: string;                     // "vnd"
  publishableKey: string;               // Stripe publishable key (for future Elements)
}
```

---

### **OrderPaymentSummaryDto** (from GET /by-order/{orderId})

```typescript
interface OrderPaymentSummaryDto {
  orderId: string;
  paymentStatus: EPaymentStatus;        // NotRequired | Pending | Paid | Failed | Refunded | PartiallyRefunded
  orderStatus: EOrderStatus;            // PENDING | CONFIRMED | READY_TO_PICKUP | ...
  paymentMethod: EPaymentMethod;        // COD | STRIPE | MOMO | PAYPAL
  totalAmount: number;                  // Total order amount
  currency: string;                     // "vnd"
  
  payments: PaymentSummary[];           // All payment attempts
  refunds: RefundSummary[];             // All refunds
  totalRefundedAmount: number;          // Sum of all successful refunds
}

interface PaymentSummary {
  id: string;                           // Internal payment id
  providerSessionId: string;            // Stripe session id
  providerPaymentId: string;            // Stripe payment intent id
  providerChargeId?: string;            // Stripe charge id (if succeeded)
  amount: number;
  status: "Created" | "Succeeded" | "Failed" | "Expired";
  failureReason?: string;               // Only if Failed
  createdAtUtc: string;                 // ISO 8601
  updatedAtUtc: string;
}

interface RefundSummary {
  id: string;
  providerRefundId: string;             // Stripe refund id
  amount: number;
  status: "Pending" | "Succeeded" | "Failed";
  reason?: string;
  initiatedByAdminUserId: string;
  createdAtUtc: string;
}
```

---

### **CreateRefundRequest**

```typescript
interface CreateRefundRequest {
  amount?: number;    // Null/0 = full refund, otherwise partial amount
  reason?: string;    // Optional reason (stored for audit)
}
```

---

### **RefundResponseDto**

```typescript
interface RefundResponseDto {
  refundId: string;                     // Stripe refund id
  status: "pending" | "succeeded" | "failed";
  amount: number;
  orderId: string;
  createdAtUtc: string;
}
```

---

## 🔄 State Machines

### **Payment Lifecycle (Order.PaymentStatus)**

```
COD Order:
┌─────────────┐
│ NotRequired │  ← terminal (no payment needed)
└─────────────┘

Stripe Happy Path:
                    ┌──────────┐
                    │ Pending  │  (session created, customer fills card form)
                    └────┬─────┘
                         │ checkout.session.completed webhook
                         ▼
                    ┌──────────┐
                    │   Paid   │  ← can confirm order now
                    └────┬─────┘
                         │ (optional) admin issues refund(s)
                         ▼
            ┌─────────────────────────┐
            │ PartiallyRefunded (0-99%│  ← more refunds possible
            └────────┬────────────────┘
                     │
                     ▼
            ┌────────────────┐
            │   Refunded     │  ← terminal (100% refunded)
            └────────────────┘

Stripe Fail Path:
                    ┌──────────┐
                    │ Pending  │
                    └────┬─────┘
                         │ checkout.session.expired OR payment_intent.payment_failed
                         ▼
                    ┌──────────┐
                    │  Failed  │  ← customer can retry payment
                    └──────────┘
```

---

### **Order Fulfillment Lifecycle (Order.OrderStatus)**

```
┌─────────┐
│ PENDING │  ← order created, staff reviewing
└────┬────┘
     │ staff confirms (only if PaymentStatus = Paid for Stripe orders)
     ▼
┌───────────┐
│ CONFIRMED │  ← ready to fulfill
└────┬──────┘
     │ staff marks ready for pickup
     ▼
┌─────────────────┐
│ READY_TO_PICKUP │  ← customer picks up
└────┬────────────┘
     │ (if delivery) staff marks in delivery
     ▼
┌────────────┐
│ IN_DELIVERY│  ← out for delivery
└────┬───────┘
     │ staff marks delivered
     ▼
┌───────────┐
│ DELIVERED │  ← complete
└────┬──────┘
     │ customer leaves review
     ▼
┌──────────┐
│ REVIEWED │  ← terminal
└──────────┘

Alternative (Cancellation):
┌─────────┐
│ PENDING │  ← customer can cancel anytime here
└────┬────┘
     │
     ▼
┌─────────────┐
│ CANCELLED   │  ← terminal (or staff can cancel)
└─────────────┘
```

---

## ⚠️ Error Handling

### **HTTP Status Codes**

| Code | Meaning | How to handle |
|------|---------|---------------|
| 200 | Success | Parse response normally |
| 201 | Created | Resource created successfully |
| 400 | Bad Request | Show error message to user (validation failed) |
| 401 | Unauthorized | Redirect to login |
| 403 | Forbidden | "You don't have permission" |
| 404 | Not Found | "Order not found" or "Payment not found" |
| 500 | Server Error | "Something went wrong, try again later" |

---

### **Common Error Messages & Solutions**

**"Order does not exist"**
- User ID mismatch or order was deleted
- Solution: Go back to order list, pick valid order

**"Order is not configured for Stripe payment"**
- PaymentMethod was set to COD, not STRIPE
- Solution: Can't use this endpoint for COD orders

**"Order is not awaiting payment"**
- PaymentStatus is no longer Pending (already Paid or Failed)
- Solution: Check payment status, might need to retry if Failed

**"Cannot confirm an order whose payment status is Pending"**
- Trying to confirm order before payment settles
- Solution: Wait for webhook (or poll payment status), then confirm

**"Refund amount exceeds remaining balance"**
- Requested refund > unrefunded amount
- Solution: Reduce refund amount

**"Stripe SecretKey is not configured"**
- Backend environment variable not set
- Solution: Contact ops/backend team

---

## ✅ Implementation Checklist

### **Phase 1: Checkout Page**
- [ ] Add payment method radio buttons (COD / Stripe)
- [ ] If Stripe selected, call `POST /api/v1/payments/stripe/checkout-session`
- [ ] On success, redirect to `response.checkoutUrl`
- [ ] Handle errors (show toast/modal)

### **Phase 2: Success/Failure Pages**
- [ ] Create `/checkout/success?session_id=...` page
- [ ] Create `/checkout/cancel` page
- [ ] Both pages: fetch `GET /api/v1/payments/by-order/{orderId}`
- [ ] Show order status based on `paymentStatus` enum
- [ ] If Pending: show loading spinner + poll every 5 seconds
- [ ] If Paid: show "Payment confirmed!" + order details
- [ ] If Failed: show "Payment failed" + retry button
- [ ] Handle edge cases (network errors, race conditions)

### **Phase 3: Order History Page**
- [ ] When displaying order, include `paymentStatus`
- [ ] Show payment status badge (✅ Paid, ⏳ Pending, ❌ Failed, ↩️ Refunded)
- [ ] For Pending: show status with loading indicator
- [ ] Add "Retry Payment" button for Failed orders

### **Phase 4: Admin Dashboard** (Refunds)
- [ ] Add refund button on order detail page
- [ ] Only show if `paymentStatus ∈ {Paid, PartiallyRefunded}`
- [ ] Modal: ask for refund amount (optional) + reason (optional)
- [ ] Call `POST /api/v1/payments/{orderId}/refund`
- [ ] Show refund status in order detail
- [ ] Show refund history (amount, timestamp, admin name)

### **Phase 5: Testing**
- [ ] Test with Stripe test card: `4242 4242 4242 4242`
- [ ] Test declined card: `4000 0000 0000 0002`
- [ ] Test 3D Secure card (requires verification): `4000 0025 0000 3155`
- [ ] Test abandoning checkout (close tab) → order stays Pending
- [ ] Test payment succeeding → order moves to Paid
- [ ] Test refunding → order moves to Refunded/PartiallyRefunded
- [ ] Test concurrent requests (e.g., fast double-click)

---

## 📊 Example Flows

### **Flow 1: Happy Path (Stripe Payment Success)**

```
Customer at checkout
  ↓ Selects "Pay with Card" (EPaymentMethod.STRIPE)
  ↓ Clicks "Pay Now"
  ↓
Frontend: POST /api/v1/payments/stripe/checkout-session
  Response: { checkoutUrl, sessionId, ... }
  ↓
Frontend: window.location.href = checkoutUrl
  ↓ Customer redirected to Stripe-hosted checkout page
  ↓ Customer fills card: "4242 4242 4242 4242"
  ↓ Customer clicks "Pay"
  ↓
Stripe processes payment (backend)
  ↓ Payment succeeds
  ↓ Stripe redirects customer to: https://yoursite.com/checkout/success?session_id=cs_test_...
  ↓
Frontend: On /checkout/success page
  ↓ Extract session_id from URL
  ↓ Poll: GET /api/v1/payments/by-order/{orderId} every 2 seconds
  ↓ Response: { paymentStatus: "Paid", ... }
  ↓
Frontend: Show success message + order details
  ↓ Display button: "Continue to Order Details"
  ↓
Backend: Order.PaymentStatus = Paid
  ↓ Admin can now confirm order for fulfillment
  ↓
✅ Complete
```

---

### **Flow 2: Payment Failure & Retry**

```
Customer at checkout
  ↓ Selects "Pay with Card"
  ↓ Uses declined card: "4000 0000 0000 0002"
  ↓
Stripe: Payment declined
  ↓ Stripe redirects to: https://yoursite.com/checkout/cancel
  ↓
Frontend: On /checkout/cancel page
  ↓ Fetch: GET /api/v1/payments/by-order/{orderId}
  ↓ Response: { paymentStatus: "Failed", ... }
  ↓
Frontend: Show error message + retry button
  ↓ "Your payment was declined. Please try another card."
  ↓
Customer clicks "Try Again"
  ↓ Goes back to checkout
  ↓ Calls POST /api/v1/payments/stripe/checkout-session again
  ↓ Gets new checkout session URL
  ↓ Tries with valid card: "4242 4242 4242 4242"
  ↓
Payment succeeds
  ↓ Order.PaymentStatus = Paid
  ↓
✅ Complete (2nd attempt successful)
```

---

### **Flow 3: Admin Refund**

```
Order detail page (Admin dashboard)
  Order: { orderId, paymentStatus: "Paid", paymentMethod: "STRIPE", amount: 250000 }
  ↓
Admin clicks "Refund" button
  ↓
Modal opens: "Refund Order"
  - Amount: [250000] (editable for partial)
  - Reason: [textarea]
  ↓
Admin enters reason: "Customer requested cancellation"
  ↓ Admin clicks "Confirm Refund"
  ↓
Frontend: POST /api/v1/payments/{orderId}/refund
  Body: { amount: 250000, reason: "Customer requested cancellation" }
  ↓
Backend: Creates RefundRecord (Pending status)
  ↓ Calls Stripe API to refund
  ↓ Returns: { refundId: "re_...", status: "pending" }
  ↓
Frontend: Show notification: "Refund initiated (pending)"
  ↓
[Later, Stripe processes refund at backend]
  ↓
Stripe sends webhook: charge.refunded
  ↓
Backend updates RefundRecord: status = "Succeeded"
  ↓ Updates Order: PaymentStatus = "Refunded"
  ↓
Frontend (polling or next page load):
  ↓ GET /api/v1/payments/by-order/{orderId}
  ↓ Response: { paymentStatus: "Refunded", refunds: [...] }
  ↓
Admin sees: "✅ Refund successful" + refund history
  ↓
✅ Complete
```

---

## 🔗 Quick Reference

### **Endpoints Summary**

| Method | Endpoint | Purpose | Auth |
|--------|----------|---------|------|
| POST | `/api/v1/payments/stripe/checkout-session` | Create Stripe checkout session | Required |
| GET | `/api/v1/payments/by-order/{orderId}` | Get payment status for order | Required |
| POST | `/api/v1/payments/{orderId}/refund` | Refund paid order (admin) | Admin |

### **Key Enums**

| Enum | Values | Notes |
|------|--------|-------|
| `EPaymentMethod` | COD=1, MOMO=2, PAYPAL=3, STRIPE=4 | User selection |
| `EPaymentStatus` | NotRequired=1, Pending=2, Paid=3, Failed=4, Refunded=5, PartiallyRefunded=6 | Order payment state |
| `EOrderStatus` | PENDING=1, CONFIRMED=2, READY_TO_PICKUP=3, IN_DELIVERY=4, DELIVERED=5, REVIEWED=6, CANCELLED=7 | Order fulfillment state |

### **Important Rules**

1. ✅ **Stripe orders require PaymentStatus = Paid before confirming**
   - For COD: PaymentStatus is always NotRequired
   - For Stripe: Must wait for PaymentStatus = Paid

2. ✅ **Webhooks handle async updates**
   - Frontend doesn't need to wait for webhook
   - But can poll payment status if needed

3. ✅ **Refunds are async**
   - RefundRecord starts Pending
   - Becomes Succeeded when webhook arrives

4. ✅ **Stripe signing secret protects webhooks**
   - Backend verifies HMAC-SHA256 signature
   - Frontend doesn't need to worry about this

---

## 📞 Support

**Backend Team**: For questions about API contracts, error codes, state machines  
**Stripe Support**: For payment processing issues, test mode vs. live mode

---

**Happy coding! 🎉**
