# Stripe Payment Integration - Frontend Delivery Guide

**Version**: 1.0  
**Date**: May 18, 2026  
**Status**: ✅ Ready for Frontend Implementation  
**Backend Branch**: 011-stripe-payment (commits: a767069, 4240da3)

---

## 📋 Overview

This document provides **everything** the frontend team needs to implement Stripe payment integration. The backend is complete, tested (29/29 passing), and ready to integrate.

### What You Need to Build

- ✅ Payment method selection UI (COD vs Stripe radio buttons)
- ✅ Stripe checkout flow (create session → redirect → handle return)
- ✅ Payment status pages (success/failure/pending)
- ✅ Order history with payment badges
- ✅ Admin refund UI (optional MVP)
- ✅ Error handling and retry flows

### What the Backend Provides

- ✅ Payment gateway abstraction (extensible for MOMO/VNPAY)
- ✅ Webhook signature verification (HMAC-SHA256)
- ✅ Idempotent webhook processing
- ✅ Full/partial refund support
- ✅ Payment history queries
- ✅ Ready for production deployment

---

## 🔌 API Endpoints

### **1. Create Checkout Session**

**Endpoint**: `POST /api/v1/payments/stripe/checkout-session`

**Purpose**: Generate a Stripe checkout URL to redirect the customer to

**Request**:
```typescript
interface CreateCheckoutSessionRequest {
  orderId: string;  // UUID of the order
}
```

**Response (201 Created)**:
```typescript
interface CreateCheckoutSessionResponse {
  checkoutUrl: string;          // "https://checkout.stripe.com/c/pay/cs_test_..."
  sessionId: string;             // "cs_test_..." - Stripe session ID
  amount: number;                // Total in minor units (e.g., 100000 = 100,000 VND)
  currency: string;              // "vnd"
  publishableKey: string;        // "pk_test_..." or "pk_live_..." (optional, for JS SDK)
}
```

**Error Responses**:
```
400 Bad Request
- "Order does not exist"
- "Order is not configured for Stripe payment"
- "Order is not awaiting payment" (already paid/failed)

401 Unauthorized
- "Invalid or expired JWT"

403 Forbidden
- "Order belongs to different user"
```

**Example**:
```typescript
async function initiateStripeCheckout(orderId: string) {
  const response = await fetch("/api/v1/payments/stripe/checkout-session", {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
      "Authorization": `Bearer ${localStorage.getItem("accessToken")}`
    },
    body: JSON.stringify({ orderId })
  });

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Checkout failed: ${error}`);
  }

  const { checkoutUrl } = await response.json();
  
  // Redirect to Stripe-hosted checkout
  window.location.href = checkoutUrl;
}
```

---

### **2. Get Payment Status by Order**

**Endpoint**: `GET /api/v1/payments/by-order/{orderId}`

**Purpose**: Query payment status and refund history after returning from Stripe

**Response (200 OK)**:
```typescript
interface PaymentStatusResponse {
  orderId: string;
  paymentStatus: EPaymentStatus;    // "NotRequired" | "Pending" | "Paid" | "Failed" | "Refunded" | "PartiallyRefunded"
  orderStatus: EOrderStatus;        // "PENDING" | "CONFIRMED" | "READY_TO_PICKUP" | "IN_DELIVERY" | "DELIVERED" | "REVIEWED" | "CANCELLED"
  
  // Payment details
  payments: {
    id: string;
    sessionId: string;              // Stripe session ID
    status: string;                 // "Succeeded" | "Expired" | "Failed"
    amount: number;                 // Amount paid
    createdAt: string;              // ISO 8601 timestamp
  }[];
  
  // Refund history
  refunds: {
    id: string;
    amount: number;
    status: string;                 // "Initiated" | "Settled" | "Failed"
    reason?: string;
    createdAt: string;
  }[];
}
```

**Error Responses**:
```
404 Not Found
- "Order not found"

401 Unauthorized
- "Invalid JWT"
```

**Example**:
```typescript
async function checkPaymentStatus(orderId: string) {
  const response = await fetch(
    `/api/v1/payments/by-order/${orderId}`,
    {
      headers: {
        "Authorization": `Bearer ${localStorage.getItem("accessToken")}`
      }
    }
  );

  if (!response.ok) {
    throw new Error("Failed to check payment status");
  }

  const paymentInfo = await response.json();
  
  // Handle based on status
  if (paymentInfo.paymentStatus === "Paid") {
    return <SuccessPage orderId={orderId} />;
  } else if (paymentInfo.paymentStatus === "Failed") {
    return <RetryPage orderId={orderId} />;
  } else if (paymentInfo.paymentStatus === "Pending") {
    return <LoadingPage />;  // Still processing webhook
  }
}
```

---

### **3. Issue Refund (Admin Only)**

**Endpoint**: `POST /api/v1/payments/{orderId}/refund`

**Purpose**: Issue full or partial refund to customer

**Request**:
```typescript
interface RefundRequest {
  amount?: number;    // Omit for full refund, specify for partial (minor units)
  reason?: string;    // Optional reason for audit trail
}
```

**Response (200 OK)**:
```typescript
interface RefundResponse {
  refundId: string;
  status: string;                 // "Initiated" | "Settled" | "Failed"
  amount: number;
  orderStatus: EPaymentStatus;    // Updated order payment status
  createdAt: string;
}
```

**Error Responses**:
```
403 Forbidden
- "Only admins can issue refunds"

400 Bad Request
- "Cannot refund more than remaining balance"
- "Order payment status does not allow refunds"

404 Not Found
- "Order or payment not found"
```

**Example**:
```typescript
async function refundOrder(orderId: string, amount?: number) {
  const response = await fetch(
    `/api/v1/payments/${orderId}/refund`,
    {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${adminToken}`
      },
      body: JSON.stringify({
        amount,
        reason: "Customer requested refund"
      })
    }
  );

  if (!response.ok) {
    const error = await response.text();
    throw new Error(`Refund failed: ${error}`);
  }

  const result = await response.json();
  alert(`Refund initiated: ${result.refundId} (status: ${result.status})`);
}
```

---

## 📊 Enums & Types

### **EPaymentMethod**
```typescript
enum EPaymentMethod {
  COD = 1,        // Cash on Delivery
  MOMO = 2,       // MOMO (future)
  PAYPAL = 3,     // PayPal (future)
  STRIPE = 4      // Stripe (MVP)
}
```

### **EPaymentStatus**
```typescript
enum EPaymentStatus {
  NotRequired = 1,       // COD - no payment needed
  Pending = 2,           // Waiting for Stripe confirmation
  Paid = 3,              // ✅ Payment confirmed
  Failed = 4,            // ❌ Payment failed, can retry
  Refunded = 5,          // 100% refunded
  PartiallyRefunded = 6  // Partial refund processed
}
```

### **EOrderStatus**
```typescript
enum EOrderStatus {
  PENDING = 1,              // Initial state
  CONFIRMED = 2,            // Only if PaymentStatus = Paid (for Stripe)
  READY_TO_PICKUP = 3,      // Ready at store
  IN_DELIVERY = 4,          // Out for delivery
  DELIVERED = 5,            // ✅ Complete
  REVIEWED = 6,             // Customer reviewed
  CANCELLED = 7             // Cancelled
}
```

### **TypeScript Interfaces** (Copy-paste ready)

```typescript
// ============ ENUMS ============

enum EPaymentMethod {
  COD = 1,
  MOMO = 2,
  PAYPAL = 3,
  STRIPE = 4
}

enum EPaymentStatus {
  NotRequired = 1,
  Pending = 2,
  Paid = 3,
  Failed = 4,
  Refunded = 5,
  PartiallyRefunded = 6
}

enum EOrderStatus {
  PENDING = 1,
  CONFIRMED = 2,
  READY_TO_PICKUP = 3,
  IN_DELIVERY = 4,
  DELIVERED = 5,
  REVIEWED = 6,
  CANCELLED = 7
}

// ============ API CONTRACTS ============

interface CreateCheckoutSessionRequest {
  orderId: string;
}

interface CreateCheckoutSessionResponse {
  checkoutUrl: string;
  sessionId: string;
  amount: number;
  currency: string;
  publishableKey?: string;
}

interface PaymentStatusResponse {
  orderId: string;
  paymentStatus: EPaymentStatus;
  orderStatus: EOrderStatus;
  payments: {
    id: string;
    sessionId: string;
    status: string;
    amount: number;
    createdAt: string;
  }[];
  refunds: {
    id: string;
    amount: number;
    status: string;
    reason?: string;
    createdAt: string;
  }[];
}

interface RefundRequest {
  amount?: number;
  reason?: string;
}

interface RefundResponse {
  refundId: string;
  status: string;
  amount: number;
  orderStatus: EPaymentStatus;
  createdAt: string;
}

// ============ DOMAIN MODELS ============

interface Order {
  id: string;
  status: EOrderStatus;
  paymentMethod: EPaymentMethod;
  paymentStatus: EPaymentStatus;
  total: number;
  items: OrderItem[];
  createdAt: string;
  updatedAt: string;
}

interface OrderItem {
  id: string;
  productId: string;
  productName: string;
  variantId: string;
  variantLabel: string;
  unitPrice: number;
  quantity: number;
}
```

---

## 🔄 State Machines

### **Payment Lifecycle (Stripe Order)**

```
START
  ↓
┌─────────────────────────────────────┐
│ User selects payment method = Stripe │
└─────────────────────────────────────┘
  ↓
┌──────────────────────────────────────────┐
│ Frontend: POST /checkout-session         │
│ Backend: Creates Stripe session          │
│ PaymentStatus = PENDING                  │
└──────────────────────────────────────────┘
  ↓
┌──────────────────────────────────────────┐
│ Frontend: Redirect to checkout.stripe.com│
│ User enters card details                 │
└──────────────────────────────────────────┘
  ↓
  ├─────────────────────────────────────┐
  │ SUCCESS PATH                        │
  ├─────────────────────────────────────┤
  │ 1. Stripe sends webhook             │
  │ 2. Backend: PaymentStatus = PAID    │
  │ 3. Frontend: User redirected back   │
  │ 4. Frontend: GET /by-order/status   │
  │ 5. Display: Success page ✅         │
  └─────────────────────────────────────┘
  │
  ├─────────────────────────────────────┐
  │ FAILURE PATH                        │
  ├─────────────────────────────────────┤
  │ 1. Payment declined                 │
  │ 2. Backend: PaymentStatus = FAILED  │
  │ 3. Frontend: User redirected back   │
  │ 4. Display: Failure page ❌         │
  │ 5. Option: Retry with new card      │
  └─────────────────────────────────────┘
  │
  ├─────────────────────────────────────┐
  │ REFUND PATH (Admin)                 │
  ├─────────────────────────────────────┤
  │ 1. Admin: POST /refund              │
  │ 2. Backend: Sends refund to Stripe  │
  │ 3. PaymentStatus = PARTIALLY_REFUND │
  │ 4. OR PaymentStatus = REFUNDED      │
  └─────────────────────────────────────┘
```

### **Order Fulfillment Lifecycle**

```
┌─────────────────────────────────────┐
│ PENDING                             │
│ (Initial state, awaiting payment)   │
└─────────────────────────────────────┘
  ↓
  ├─ For COD: Skip to CONFIRMED
  │
  ├─ For Stripe: Wait for PAID status
  │
  ↓
┌─────────────────────────────────────┐
│ CONFIRMED                           │
│ (Payment received, ready to make)   │
└─────────────────────────────────────┘
  ↓
┌─────────────────────────────────────┐
│ READY_TO_PICKUP                     │
│ (Order prepared, waiting at store)  │
└─────────────────────────────────────┘
  ↓
┌─────────────────────────────────────┐
│ IN_DELIVERY                         │
│ (Shipped out)                       │
└─────────────────────────────────────┘
  ↓
┌─────────────────────────────────────┐
│ DELIVERED                           │
│ (✅ Complete)                       │
└─────────────────────────────────────┘
  ↓
┌─────────────────────────────────────┐
│ REVIEWED                            │
│ (Customer left feedback)            │
└─────────────────────────────────────┘
```

---

## ⚠️ Error Handling

### **Common Errors & Solutions**

| HTTP Code | Error Message | Meaning | Solution |
|-----------|---------------|---------|----------|
| 400 | "Order does not exist" | Invalid orderId | Verify orderId exists |
| 400 | "Order is not configured for Stripe payment" | paymentMethod ≠ STRIPE | Check order.paymentMethod |
| 400 | "Order is not awaiting payment" | Already paid or failed | Can't re-checkout; show retry UI |
| 401 | "Unauthorized" | JWT missing/invalid | Re-authenticate user |
| 403 | "Forbidden" | Trying to access another user's order | Check user ownership |
| 404 | "Not found" | Order/payment doesn't exist | Handle gracefully |
| 422 | "Signature verification failed" | (Webhook only) | Stripe signature invalid |
| 500 | Server error | Temporary backend issue | Retry with exponential backoff |

### **Error Handling Pattern**

```typescript
async function safeApiCall<T>(
  url: string,
  options?: RequestInit
): Promise<T> {
  try {
    const response = await fetch(url, options);

    // Handle non-200 responses
    if (!response.ok) {
      const errorText = await response.text();
      
      switch (response.status) {
        case 400:
          throw new BadRequestError(errorText);
        case 401:
          throw new UnauthorizedError("Please log in again");
        case 403:
          throw new ForbiddenError("You don't have access");
        case 404:
          throw new NotFoundError("Resource not found");
        default:
          throw new ApiError(`Server error: ${response.status}`);
      }
    }

    return await response.json();
  } catch (error) {
    if (error instanceof ApiError) throw error;
    throw new NetworkError("Failed to connect to server");
  }
}

// Usage
try {
  const result = await safeApiCall<CreateCheckoutSessionResponse>(
    "/api/v1/payments/stripe/checkout-session",
    {
      method: "POST",
      headers: { "Authorization": `Bearer ${token}` },
      body: JSON.stringify({ orderId })
    }
  );
  window.location.href = result.checkoutUrl;
} catch (error) {
  if (error instanceof BadRequestError) {
    showError("Invalid order. Please try again.");
  } else if (error instanceof UnauthorizedError) {
    redirectToLogin();
  } else {
    showError("Payment initiation failed. Try again later.");
  }
}
```

---

## ✅ Implementation Checklist

### **Phase 1: Checkout Flow** (Days 1-2)

- [ ] Add payment method selection (radio buttons: COD vs Stripe)
- [ ] Create `CreateCheckoutSessionRequest` API call
- [ ] Redirect to Stripe checkout on success
- [ ] Handle checkout URL in state management
- [ ] Test with backend sandbox environment

### **Phase 2: Success/Failure Pages** (Days 2-3)

- [ ] Create success page (show order details, confirmation)
- [ ] Create failure page (show error, retry button)
- [ ] Create pending page (show loading spinner, auto-poll status)
- [ ] Implement `GET /by-order/{orderId}` to fetch payment status
- [ ] Handle redirect parameters (session_id, etc.)
- [ ] Test with Stripe test cards

### **Phase 3: Order History Integration** (Day 3)

- [ ] Display payment status badges in order list
- [ ] Show payment status in order detail page
- [ ] Display refund history if applicable
- [ ] Color-code statuses (green=paid, red=failed, yellow=pending)

### **Phase 4: Admin Refund UI** (Day 4, Optional MVP)

- [ ] Create refund modal/form for admin
- [ ] Input: amount (optional for full refund), reason
- [ ] Call `POST /refund` endpoint
- [ ] Show refund confirmation
- [ ] Update order payment status in real-time

### **Phase 5: Testing & QA** (Day 5)

- [ ] Test happy path (successful payment)
- [ ] Test failure path (card declined)
- [ ] Test retry flow (expired session)
- [ ] Test with all Stripe test cards (see below)
- [ ] Test error messages
- [ ] Performance test (no slow loads)
- [ ] Security test (no sensitive data in logs)

---

## 🧪 Testing with Stripe Test Cards

### **Test Card Numbers** (Use any future expiry + CVC 123)

| Scenario | Card Number | Expected Result |
|----------|-------------|-----------------|
| ✅ Success | 4242 4242 4242 4242 | Payment succeeds immediately |
| ❌ Declined | 4000 0000 0000 0002 | Payment is declined |
| 🔐 3D Secure | 4000 0025 0000 3155 | Requires 3D Secure authentication |
| 🔐 3D Secure Fail | 4000 0025 0000 3163 | 3D Secure authentication fails |

**For all cards**:
- Expiry: Any future date (e.g., 12/26)
- CVC: Any 3 digits (e.g., 123)
- Name: Any text (e.g., "Test User")

### **Webhook Testing (Local Development)**

```bash
# 1. Start Stripe CLI listener
stripe listen --forward-to http://localhost:8002/api/v1/payments/stripe/webhook

# 2. Set webhook signing secret in environment
export Stripe__WebhookSigningSecret="whsec_..." # from CLI output

# 3. Run tests
# - Create checkout session
# - Complete payment with test card
# - CLI automatically forwards event to backend
# - Backend processes webhook asynchronously
# - Query /by-order/{orderId} to see updated status
```

---

## 📝 Example Implementation

### **React Hook for Stripe Checkout**

```typescript
// hooks/useStripeCheckout.ts

import { useState } from 'react';

interface CheckoutState {
  loading: boolean;
  error: string | null;
  checkoutUrl: string | null;
}

export function useStripeCheckout() {
  const [state, setState] = useState<CheckoutState>({
    loading: false,
    error: null,
    checkoutUrl: null,
  });

  const initiateCheckout = async (orderId: string) => {
    setState({ loading: true, error: null, checkoutUrl: null });

    try {
      const token = localStorage.getItem('accessToken');
      const response = await fetch('/api/v1/payments/stripe/checkout-session', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': `Bearer ${token}`,
        },
        body: JSON.stringify({ orderId }),
      });

      if (!response.ok) {
        const error = await response.text();
        throw new Error(error);
      }

      const { checkoutUrl } = await response.json();
      setState({ loading: false, error: null, checkoutUrl });

      // Redirect to Stripe
      window.location.href = checkoutUrl;
    } catch (error) {
      setState({
        loading: false,
        error: error instanceof Error ? error.message : 'Checkout failed',
        checkoutUrl: null,
      });
    }
  };

  return { ...state, initiateCheckout };
}

// Usage in component
function CheckoutButton({ orderId }: { orderId: string }) {
  const { loading, error, initiateCheckout } = useStripeCheckout();

  return (
    <>
      {error && <div className="alert alert-error">{error}</div>}
      <button
        onClick={() => initiateCheckout(orderId)}
        disabled={loading}
      >
        {loading ? 'Redirecting to Stripe...' : 'Pay with Card'}
      </button>
    </>
  );
}
```

### **Payment Status Component**

```typescript
// components/PaymentStatus.tsx

import { useEffect, useState } from 'react';
import { EPaymentStatus } from '@/types/payment';

interface PaymentStatusProps {
  orderId: string;
  initialStatus?: EPaymentStatus;
}

export function PaymentStatus({ orderId, initialStatus }: PaymentStatusProps) {
  const [status, setStatus] = useState(initialStatus);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    const fetchStatus = async () => {
      try {
        const token = localStorage.getItem('accessToken');
        const response = await fetch(`/api/v1/payments/by-order/${orderId}`, {
          headers: { 'Authorization': `Bearer ${token}` },
        });

        if (!response.ok) throw new Error('Failed to fetch status');

        const data = await response.json();
        setStatus(data.paymentStatus);
        setLoading(false);

        // Stop polling if status is terminal
        if (
          data.paymentStatus === EPaymentStatus.Paid ||
          data.paymentStatus === EPaymentStatus.Failed ||
          data.paymentStatus === EPaymentStatus.Refunded
        ) {
          return;
        }

        // Otherwise, poll again in 3 seconds
        setTimeout(fetchStatus, 3000);
      } catch (error) {
        setLoading(false);
      }
    };

    fetchStatus();
  }, [orderId]);

  const statusConfig = {
    [EPaymentStatus.Pending]: {
      icon: '⏳',
      text: 'Payment processing...',
      color: 'text-yellow-500',
    },
    [EPaymentStatus.Paid]: {
      icon: '✅',
      text: 'Payment confirmed',
      color: 'text-green-500',
    },
    [EPaymentStatus.Failed]: {
      icon: '❌',
      text: 'Payment failed',
      color: 'text-red-500',
    },
    [EPaymentStatus.PartiallyRefunded]: {
      icon: '💰',
      text: 'Partially refunded',
      color: 'text-blue-500',
    },
    [EPaymentStatus.Refunded]: {
      icon: '↩️',
      text: 'Fully refunded',
      color: 'text-purple-500',
    },
    [EPaymentStatus.NotRequired]: {
      icon: '💵',
      text: 'Pay on delivery',
      color: 'text-gray-500',
    },
  };

  if (loading) return <div>Loading...</div>;

  const config = statusConfig[status!];
  return (
    <p className={config.color}>
      {config.icon} {config.text}
    </p>
  );
}
```

---

## 🚀 Deployment Checklist

### **Before Going Live**

- [ ] Backend is deployed to staging
- [ ] All 29 unit tests pass
- [ ] Frontend integration tested end-to-end
- [ ] Stripe account is live (not test mode)
- [ ] Live keys are in production environment variables
- [ ] Webhook endpoint is registered in Stripe dashboard
- [ ] HTTPS is enforced (required for Stripe)
- [ ] Error handling tested with real cards
- [ ] Refund flow tested
- [ ] Payment history displays correctly
- [ ] No sensitive data logged (card numbers, tokens, etc.)

---

## 📞 Support & Questions

**Backend API Documentation**:
- `README.md` - General project overview
- `specs/011-stripe-payment/FRONTEND_INTEGRATION_GUIDE.md` - Detailed integration guide
- `PAYMENT_ARCHITECTURE_REVIEW.md` - Architecture decisions

**Stripe Documentation**:
- [Stripe Checkout Docs](https://stripe.com/docs/payments/checkout)
- [Stripe Test Cards](https://stripe.com/docs/testing)
- [Webhook Security](https://stripe.com/docs/webhooks/signatures)

**Common Issues**:

| Issue | Solution |
|-------|----------|
| "Order not found" error | Verify orderId is valid and belongs to authenticated user |
| Checkout URL is invalid | Check that backend is running and Stripe config is correct |
| Payment succeeds but no order created | Call GET /by-order/{orderId} to check status (webhook might be delayed) |
| Webhook not triggering | Use Stripe CLI: `stripe listen --forward-to http://localhost:8002/...` |
| CORS errors | Backend enables CORS for your frontend domain |

---

## 🎯 Success Criteria

✅ **MVP is complete when**:

1. Customer can select "Pay with Card" at checkout
2. Customer is redirected to Stripe-hosted checkout
3. Payment succeeds → order status updates to PAID
4. Payment fails → customer sees error and can retry
5. Admin can view payment history
6. All error cases are handled gracefully

✅ **Optional (Post-MVP)**:

1. Admin refund UI implemented
2. Real-time payment status updates with WebSocket
3. Email notifications on payment events
4. Support for additional payment methods (MOMO, VNPAY)

---

## 📅 Timeline

| Phase | Days | Owner | Tasks |
|-------|------|-------|-------|
| Phase 1 | 1-2 | Frontend | Checkout flow, Stripe redirect |
| Phase 2 | 2-3 | Frontend | Success/failure pages, status polling |
| Phase 3 | 3 | Frontend | Order history integration |
| Phase 4 | 4 | Frontend (opt) | Admin refund UI |
| Phase 5 | 5 | QA | Testing with all scenarios |

**Total**: ~5 days (3 days critical path + 2 days optional + testing)

---

**✅ Backend Status**: Production-ready, tested (29/29 passing)  
**📊 Frontend Status**: Ready for implementation  
**🚀 Expected Delivery**: May 23, 2026 (5 business days)

---

Generated from backend branch `011-stripe-payment` (commits a767069, 4240da3)
