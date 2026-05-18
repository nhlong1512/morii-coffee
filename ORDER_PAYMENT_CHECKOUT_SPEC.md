# Order & Payment Checkout Specification

**Version**: 1.0  
**Date**: May 18, 2026  
**Audience**: Frontend Development Team  
**Status**: ✅ Ready for Implementation

---

## 📋 Table of Contents

1. [Checkout Flow Overview](#-checkout-flow-overview)
2. [Enums & Constants](#-enums--constants)
3. [API Contracts](#-api-contracts)
4. [Business Rules](#-business-rules)
5. [State Transitions](#-state-transitions)
6. [Data Models](#-data-models)
7. [Validation Rules](#-validation-rules)
8. [Error Scenarios](#-error-scenarios)

---

## 🛒 Checkout Flow Overview

### **High-Level Flow**

```
1. Customer adds items to cart
                    ↓
2. Customer proceeds to checkout
                    ↓
3. Customer selects payment method (COD or Stripe)
                    ↓
4. IF COD:
   - Create order immediately
   - Order.PaymentStatus = NotRequired
   - Order.Status = PENDING
                    ↓
   IF Stripe:
   - Create checkout session
   - Order NOT created yet
   - PaymentStatus = Pending (waiting for Stripe)
                    ↓
5. Customer confirms payment
                    ↓
6. Backend confirms payment via webhook
                    ↓
7. Order status updates
                    ↓
8. Order fulfillment begins
```

### **Key Difference from Previous Flow**

| Aspect | Before | After |
|--------|--------|-------|
| Order Creation | Created before payment | Created AFTER payment confirmed (Stripe) |
| Abandoned Sessions | Create orphan orders | No orphan orders (safe) |
| Cart Availability | Cleared immediately | Stays until payment confirmed |
| Payment Workflow | Synchronous | Asynchronous (webhook-driven) |

---

## 🎯 Enums & Constants

### **1. EPaymentMethod** (How to pay)

```typescript
enum EPaymentMethod {
  COD = 1,          // Cash on Delivery - pay at store
  MOMO = 2,         // MOMO e-wallet (future)
  PAYPAL = 3,       // PayPal (future)
  STRIPE = 4        // Online card payment (MVP)
}

// Usage in UI
const paymentMethods = [
  { label: "💵 Pay on Delivery", value: EPaymentMethod.COD },
  { label: "💳 Pay with Card", value: EPaymentMethod.STRIPE }
];
```

### **2. EPaymentStatus** (Payment state)

```typescript
enum EPaymentStatus {
  NotRequired = 1,       // COD orders - no payment needed
  Pending = 2,           // Stripe session created, waiting for customer to pay
  Paid = 3,              // ✅ Payment confirmed by Stripe
  Failed = 4,            // ❌ Payment failed - can retry or cancel
  Refunded = 5,          // 100% refunded (terminal state)
  PartiallyRefunded = 6  // Partial refund issued
}

// State transitions
const paymentStateTransitions = {
  NotRequired: [],                              // Terminal for COD
  Pending: [EPaymentStatus.Paid, EPaymentStatus.Failed],
  Paid: [EPaymentStatus.PartiallyRefunded, EPaymentStatus.Refunded],
  Failed: [EPaymentStatus.Pending],             // Can retry
  PartiallyRefunded: [EPaymentStatus.Refunded],
  Refunded: []                                  // Terminal
};
```

### **3. EOrderStatus** (Fulfillment state)

```typescript
enum EOrderStatus {
  PENDING = 1,              // Initial - order created, staff reviewing
  CONFIRMED = 2,            // ✅ Confirmed for fulfillment
  READY_TO_PICKUP = 3,      // Ready at store
  IN_DELIVERY = 4,          // Out for delivery
  DELIVERED = 5,            // ✅ Complete
  REVIEWED = 6,             // Customer left review
  CANCELLED = 7             // Cancelled by customer or staff
}

// State transitions (rank-based, forward-only)
const orderStateTransitions = {
  PENDING: [
    EOrderStatus.CONFIRMED,
    EOrderStatus.CANCELLED
  ],
  CONFIRMED: [
    EOrderStatus.READY_TO_PICKUP,
    EOrderStatus.CANCELLED
  ],
  READY_TO_PICKUP: [EOrderStatus.IN_DELIVERY],
  IN_DELIVERY: [EOrderStatus.DELIVERED],
  DELIVERED: [EOrderStatus.REVIEWED],
  REVIEWED: [],               // Terminal
  CANCELLED: []               // Terminal
};
```

### **4. ERefundStatus** (Refund state)

```typescript
enum ERefundStatus {
  Initiated = 1,   // Refund request sent to Stripe, waiting for confirmation
  Settled = 2,     // ✅ Refund confirmed by Stripe webhook
  Failed = 3       // ❌ Refund failed at Stripe
}
```

---

## 📡 API Contracts

### **1. Create Order (COD Only)**

**Endpoint**: `POST /api/v1/orders`

**Purpose**: Create order with COD payment method

**Request**:
```typescript
interface CreateOrderRequest {
  // Delivery info
  fullName: string;              // "Nguyễn Văn A"
  phoneNumber: string;           // "0775504619"
  address: string;               // "1170/61 3 Tháng 2, Q.11, HCM"
  notes?: string;                // "No ice, extra sugar"
  
  // Payment method
  paymentMethod: EPaymentMethod; // EPaymentMethod.COD
  
  // Saved profile
  saveDeliveryProfile?: boolean;  // Default: false
}
```

**Response (201 Created)**:
```typescript
interface OrderResponse {
  id: string;                    // UUID
  orderNumber: string;           // "MRC-20260518-001"
  status: EOrderStatus;          // PENDING
  paymentMethod: EPaymentMethod; // COD
  paymentStatus: EPaymentStatus; // NotRequired
  
  // Monetary details
  total: number;                 // 250000 (in minor units)
  currency: string;              // "vnd"
  
  // Items
  items: OrderItem[];
  
  // Delivery
  deliveryInfo: {
    fullName: string;
    phoneNumber: string;
    address: string;
    notes?: string;
  };
  
  // Timestamps
  createdAt: string;             // ISO 8601
  updatedAt: string;
}
```

**Error Responses**:
```
400 Bad Request
- "Cart is empty"
- "Invalid delivery address"
- "Phone number is invalid"

401 Unauthorized
- "JWT missing or invalid"

409 Conflict
- "User already has a pending COD order"
```

**Example**:
```typescript
// Create COD order
const response = await fetch("/api/v1/orders", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
  },
  body: JSON.stringify({
    fullName: "Nguyễn Văn A",
    phoneNumber: "0775504619",
    address: "1170/61 3 Tháng 2, Q.11, HCM",
    notes: "No ice",
    paymentMethod: EPaymentMethod.COD,
    saveDeliveryProfile: true
  })
});

const order = await response.json();
// order.paymentStatus === "NotRequired"
// order.status === "PENDING"
// order.id is ready to use
```

---

### **2. Create Stripe Checkout Session**

**Endpoint**: `POST /api/v1/payments/stripe/checkout-session`

**Purpose**: Create Stripe session for payment (Order NOT created yet)

**Request**:
```typescript
interface CreateCheckoutSessionRequest {
  orderId: string;  // UUID - must be valid and belong to user
}
```

**Response (201 Created)**:
```typescript
interface CreateCheckoutSessionResponse {
  checkoutUrl: string;          // "https://checkout.stripe.com/c/pay/cs_test_..."
  sessionId: string;            // "cs_test_..." Stripe session ID
  
  // Order summary for display
  amount: number;               // 250000 (total in minor units)
  currency: string;             // "vnd"
  
  // Optional: Stripe publishable key for client-side usage
  publishableKey?: string;      // "pk_test_..." or "pk_live_..."
}
```

**Error Responses**:
```
400 Bad Request
- "Order does not exist"
- "Order is not configured for Stripe payment" (paymentMethod ≠ STRIPE)
- "Order is not awaiting payment" (paymentStatus ≠ Pending)
- "Order already has a payment"

401 Unauthorized
- "JWT missing or invalid"

403 Forbidden
- "Order belongs to different user"

500 Server Error
- "Failed to create Stripe session" (temporary issue)
```

**Example**:
```typescript
// First, create an order with Stripe payment method
const orderResponse = await fetch("/api/v1/orders", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
  },
  body: JSON.stringify({
    fullName: "Nguyễn Văn A",
    phoneNumber: "0775504619",
    address: "1170/61 3 Tháng 2, Q.11, HCM",
    paymentMethod: EPaymentMethod.STRIPE
  })
});

const order = await orderResponse.json();

// Then create checkout session
const checkoutResponse = await fetch("/api/v1/payments/stripe/checkout-session", {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "Authorization": `Bearer ${token}`
  },
  body: JSON.stringify({ orderId: order.id })
});

const { checkoutUrl } = await checkoutResponse.json();

// Redirect customer to Stripe
window.location.href = checkoutUrl;
```

---

### **3. Get Payment Status by Order**

**Endpoint**: `GET /api/v1/payments/by-order/{orderId}`

**Purpose**: Check current payment and refund status

**Response (200 OK)**:
```typescript
interface PaymentStatusResponse {
  orderId: string;
  
  // Payment state
  paymentStatus: EPaymentStatus;  // Paid | Failed | Pending | etc.
  
  // Fulfillment state
  orderStatus: EOrderStatus;      // CONFIRMED | PENDING | etc.
  
  // Payment transactions
  payments: {
    id: string;
    sessionId: string;            // Stripe session ID
    status: string;               // "Succeeded" | "Expired" | "Failed"
    amount: number;               // Amount paid
    createdAt: string;            // ISO 8601
  }[];
  
  // Refunds
  refunds: {
    id: string;
    amount: number;               // Refund amount
    status: ERefundStatus;        // "Initiated" | "Settled" | "Failed"
    reason?: string;              // Admin reason
    createdAt: string;
  }[];
}
```

**Error Responses**:
```
404 Not Found
- "Order not found"

401 Unauthorized
- "JWT missing or invalid"

403 Forbidden
- "Order belongs to different user" (non-admins can only see own orders)
```

**Example**:
```typescript
// After returning from Stripe
const response = await fetch(
  `/api/v1/payments/by-order/${orderId}`,
  {
    headers: {
      "Authorization": `Bearer ${token}`
    }
  }
);

const status = await response.json();

// Show different UI based on status
if (status.paymentStatus === EPaymentStatus.Paid) {
  // ✅ Show success page
  showSuccessPage(status);
} else if (status.paymentStatus === EPaymentStatus.Failed) {
  // ❌ Show failure page with retry option
  showFailurePage(status);
} else if (status.paymentStatus === EPaymentStatus.Pending) {
  // ⏳ Still processing, poll again in 3 seconds
  setTimeout(() => fetchStatus(), 3000);
}
```

---

### **4. Get Order Details**

**Endpoint**: `GET /api/v1/orders/{orderId}`

**Purpose**: Fetch complete order with payment info

**Response (200 OK)**:
```typescript
interface OrderDetailResponse {
  id: string;
  orderNumber: string;
  
  // Status
  status: EOrderStatus;
  paymentStatus: EPaymentStatus;
  paymentMethod: EPaymentMethod;
  
  // Amounts
  total: number;
  currency: string;
  
  // Items in order
  items: {
    id: string;
    productId: string;
    productName: string;
    variantId: string;
    variantLabel: string;
    unitPrice: number;
    quantity: number;
    subtotal: number;
  }[];
  
  // Delivery
  deliveryInfo: {
    fullName: string;
    phoneNumber: string;
    address: string;
    notes?: string;
  };
  
  // Payment details (if applicable)
  payment?: {
    id: string;
    sessionId: string;
    status: string;
    paidAt?: string;
  };
  
  // Refund details (if applicable)
  refunds?: {
    id: string;
    amount: number;
    status: ERefundStatus;
    reason?: string;
    createdAt: string;
  }[];
  
  // Timestamps
  createdAt: string;
  updatedAt: string;
}
```

---

## 💼 Business Rules

### **Order Creation Rules**

```typescript
// Rule 1: Cart must not be empty
if (cart.items.length === 0) {
  throw new Error("Cart is empty");
}

// Rule 2: Delivery info is required
if (!fullName || !phoneNumber || !address) {
  throw new Error("Delivery info is required");
}

// Rule 3: Phone number must be valid
if (!isValidPhone(phoneNumber)) {
  throw new Error("Phone number is invalid");
}

// Rule 4: User cannot have multiple pending orders of same payment method
const existingOrder = await ordersRepo.findPendingByUser(userId, paymentMethod);
if (existingOrder && paymentMethod === EPaymentMethod.COD) {
  throw new Error("You already have a pending COD order");
}

// Rule 5: Order total must match cart total
const orderTotal = calculateTotal(items);
if (orderTotal !== cartTotal) {
  throw new Error("Order total mismatch");
}
```

### **Payment Status Rules**

```typescript
// Rule 1: COD orders ALWAYS have PaymentStatus = NotRequired
if (paymentMethod === EPaymentMethod.COD) {
  paymentStatus = EPaymentStatus.NotRequired;
}

// Rule 2: Stripe orders start with PaymentStatus = Pending
if (paymentMethod === EPaymentMethod.STRIPE) {
  paymentStatus = EPaymentStatus.Pending;
}

// Rule 3: Only PAID Stripe orders can be confirmed
if (orderStatus === EOrderStatus.PENDING && paymentMethod === EPaymentMethod.STRIPE) {
  if (paymentStatus !== EPaymentStatus.Paid) {
    throw new Error("Cannot confirm unpaid Stripe order");
  }
}

// Rule 4: COD orders can be confirmed immediately
if (orderStatus === EOrderStatus.PENDING && paymentMethod === EPaymentMethod.COD) {
  // Can immediately move to CONFIRMED or other states
  // Payment check is skipped
}

// Rule 5: Payment status is immutable once terminal
const terminalStatuses = [
  EPaymentStatus.Refunded,
  EPaymentStatus.NotRequired
];
if (terminalStatuses.includes(currentPaymentStatus)) {
  throw new Error("Cannot change terminal payment status");
}
```

### **Refund Rules**

```typescript
// Rule 1: Only Paid orders can be refunded
if (paymentStatus !== EPaymentStatus.Paid && 
    paymentStatus !== EPaymentStatus.PartiallyRefunded) {
  throw new Error("Order is not eligible for refund");
}

// Rule 2: Refund amount cannot exceed remaining balance
const refundedAmount = refunds.reduce((sum, r) => sum + r.amount, 0);
const remainingBalance = payment.amount - refundedAmount;
if (refundAmount > remainingBalance) {
  throw new Error("Cannot refund more than remaining balance");
}

// Rule 3: Full refund sets PaymentStatus = Refunded (terminal)
if (refundAmount === remainingBalance) {
  paymentStatus = EPaymentStatus.Refunded;
}

// Rule 4: Partial refund sets PaymentStatus = PartiallyRefunded
if (refundAmount < remainingBalance) {
  paymentStatus = EPaymentStatus.PartiallyRefunded;
}

// Rule 5: Only admins can issue refunds
if (!user.isAdmin) {
  throw new Error("Only admins can issue refunds");
}
```

---

## 🔄 State Transitions

### **Order Status Transition Graph**

```
                    ┌─── PENDING ───┐
                    │       │       │
                    │       ▼       ▼
                    │   CONFIRMED  CANCELLED
                    │       │
                    │       ▼
                    │  READY_TO_PICKUP
                    │       │
                    │       ▼
                    │   IN_DELIVERY
                    │       │
                    │       ▼
                    │   DELIVERED
                    │       │
                    │       ▼
                    │    REVIEWED
                    │      (Terminal)
                    │
                    └─ CANCELLED (Terminal)

Transition Rules:
- Can only move FORWARD (higher rank)
- Cannot skip more than a few steps (see endpoint docs)
- CANCELLED is terminal
- REVIEWED is terminal
```

### **Payment Status Transition Graph**

```
For COD:
NotRequired ──────────── (Terminal)

For Stripe:
Pending ─┬─→ Paid ─┬─→ PartiallyRefunded ─→ Refunded
         │        │                          (Terminal)
         │        └──────→ Refunded
         │                (Terminal)
         │
         └─→ Failed ──→ Pending (retry)
```

---

## 📊 Data Models

### **Order Entity**

```typescript
interface Order {
  // Identity
  id: string;                       // UUID
  orderNumber: string;              // "MRC-20260518-001"
  
  // Owner
  userId: string;                   // UUID
  
  // Status
  status: EOrderStatus;             // PENDING, CONFIRMED, etc.
  paymentStatus: EPaymentStatus;    // NotRequired, Pending, Paid, etc.
  paymentMethod: EPaymentMethod;    // COD, STRIPE, etc.
  
  // Monetary
  total: number;                    // Total in minor units (e.g., 250000 VND)
  currency: string;                 // "vnd"
  
  // Items
  items: OrderItem[];               // Line items in order
  
  // Delivery
  deliveryInfo: {
    fullName: string;
    phoneNumber: string;
    address: string;
    notes?: string;
  };
  
  // Payment reference (if Stripe)
  stripePaymentIntentId?: string;   // Stripe PI ID
  stripeChargeId?: string;          // Stripe charge ID
  
  // Timestamps
  createdAt: Date;
  updatedAt: Date;
  confirmedAt?: Date;               // When moved to CONFIRMED
  deliveredAt?: Date;               // When moved to DELIVERED
}

interface OrderItem {
  id: string;                       // UUID
  orderId: string;                  // Parent order
  
  // Product reference
  productId: string;
  productName: string;              // "Cà phê sữa"
  
  // Variant reference
  variantId: string;
  variantLabel: string;             // "Lớn" (Large)
  
  // Pricing
  unitPrice: number;                // Price per unit (minor units)
  quantity: number;                 // How many
  subtotal: number;                 // unitPrice * quantity
  
  // Timestamps
  createdAt: Date;
}
```

### **Payment Entity**

```typescript
interface Payment {
  // Identity
  id: string;                       // UUID
  orderId: string;                  // Parent order
  
  // Session reference
  sessionId: string;                // Stripe session ID
  
  // Payment intent reference
  paymentIntentId?: string;         // Stripe PI ID
  chargeId?: string;                // Stripe charge ID
  
  // Status
  status: string;                   // "Succeeded" | "Expired" | "Failed"
  
  // Amount
  amount: number;                   // Total paid (minor units)
  currency: string;                 // "vnd"
  
  // Refunds
  refunds: Refund[];                // Array of refund records
  
  // Timestamps
  createdAt: Date;
  updatedAt: Date;
  paidAt?: Date;                    // When payment succeeded
}

interface Refund {
  id: string;                       // UUID
  paymentId: string;                // Parent payment
  
  // Reference
  stripeRefundId?: string;          // Stripe refund ID
  
  // Amount
  amount: number;                   // Refund amount (minor units)
  
  // Status
  status: ERefundStatus;            // Initiated | Settled | Failed
  
  // Admin note
  reason?: string;                  // Why refund issued
  
  // Timestamps
  createdAt: Date;
  settledAt?: Date;                 // When webhook confirmed
}
```

---

## ✅ Validation Rules

### **Delivery Info Validation**

```typescript
interface DeliveryValidation {
  fullName: {
    required: true,
    minLength: 2,
    maxLength: 100,
    pattern: /^[a-zàáạảãăằắặẳẵâầấậẩẫèéẹẻẽêềếệểễìíịỉĩòóọỏõôồốộổỗơờớợởỡùúụủũưừứựửữỳýỵỷỹđ\s\-]+$/i  // Vietnamese chars
  },
  
  phoneNumber: {
    required: true,
    pattern: /^(0[1-9]\d{8}|84[1-9]\d{8})$/,  // Vietnam format (10-11 digits)
    minLength: 10,
    maxLength: 11
  },
  
  address: {
    required: true,
    minLength: 5,
    maxLength: 255
  },
  
  notes: {
    required: false,
    maxLength: 500
  }
}
```

### **Order Total Validation**

```typescript
// Cart total must match order total
const cartTotal = cart.items.reduce((sum, item) => {
  return sum + (item.unitPrice * item.quantity);
}, 0);

const orderTotal = order.items.reduce((sum, item) => {
  return sum + (item.unitPrice * item.quantity);
}, 0);

if (cartTotal !== orderTotal) {
  throw new Error("Order total mismatch");
}

// Amount must be > 0
if (orderTotal <= 0) {
  throw new Error("Order total must be greater than 0");
}

// Amount must be <= max (e.g., 100,000,000 VND)
const MAX_ORDER_AMOUNT = 100_000_000;
if (orderTotal > MAX_ORDER_AMOUNT) {
  throw new Error("Order amount exceeds maximum");
}
```

---

## ⚠️ Error Scenarios

### **Common Error Cases**

| Scenario | HTTP | Error Message | Frontend Action |
|----------|------|---------------|-----------------|
| Cart empty | 400 | "Cart is empty" | Show empty cart message |
| Invalid phone | 400 | "Phone number is invalid" | Highlight phone field |
| Order not found | 404 | "Order not found" | Redirect to orders list |
| Already pending | 409 | "You already have a pending COD order" | Show conflict message |
| Unpaid Stripe | 400 | "Order is not awaiting payment" | Redirect to checkout |
| User mismatch | 403 | "Order belongs to different user" | Redirect to home |
| JWT invalid | 401 | "Unauthorized" | Redirect to login |
| Stripe down | 500 | "Failed to create Stripe session" | Retry after 5 seconds |

### **Frontend Error Handling Pattern**

```typescript
async function createOrder(request: CreateOrderRequest) {
  try {
    const response = await fetch("/api/v1/orders", {
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        "Authorization": `Bearer ${token}`
      },
      body: JSON.stringify(request)
    });

    if (!response.ok) {
      const error = await response.text();
      
      // Parse error and handle
      switch (response.status) {
        case 400:
          // Bad request - validation error
          if (error.includes("empty")) {
            throw new ValidationError("Please add items to cart");
          } else if (error.includes("phone")) {
            throw new ValidationError("Phone number is invalid");
          }
          throw new ValidationError(error);
          
        case 401:
          // Unauthorized - redirect to login
          redirectToLogin();
          break;
          
        case 403:
          // Forbidden - access denied
          throw new PermissionError("You don't have access to this order");
          
        case 409:
          // Conflict - duplicate order
          throw new ConflictError(error);
          
        case 500:
          // Server error - retry
          throw new RetryableError("Server error, please try again");
          
        default:
          throw new ApiError(`Unexpected error: ${response.status}`);
      }
    }

    return await response.json();
  } catch (error) {
    console.error("Order creation failed:", error);
    throw error;
  }
}
```

---

## 🎯 Implementation Workflow

### **For Stripe Orders**

```typescript
// Step 1: Create order with Stripe payment method
const order = await createOrder({
  fullName: "Nguyễn Văn A",
  phoneNumber: "0775504619",
  address: "1170/61 3 Tháng 2, Q.11, HCM",
  paymentMethod: EPaymentMethod.STRIPE
});
// order.paymentStatus = "Pending"
// order.status = "PENDING"

// Step 2: Create checkout session
const checkout = await createCheckoutSession({
  orderId: order.id
});

// Step 3: Redirect to Stripe
window.location.href = checkout.checkoutUrl;

// Step 4: After payment, check status
const status = await getPaymentStatus(order.id);
// status.paymentStatus = "Paid" or "Failed" or "Pending"

// Step 5: Show appropriate UI
if (status.paymentStatus === "Paid") {
  showSuccessPage(order);
} else if (status.paymentStatus === "Failed") {
  showFailurePage(order);
}
```

### **For COD Orders**

```typescript
// Step 1: Create order with COD payment method
const order = await createOrder({
  fullName: "Nguyễn Văn A",
  phoneNumber: "0775504619",
  address: "1170/61 3 Tháng 2, Q.11, HCM",
  paymentMethod: EPaymentMethod.COD
});
// order.paymentStatus = "NotRequired" (immediately)
// order.status = "PENDING"

// Step 2: Show success page immediately
showSuccessPage(order);

// No Stripe checkout needed for COD
```

---

## 📋 TypeScript Types (Copy-Paste Ready)

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

enum ERefundStatus {
  Initiated = 1,
  Settled = 2,
  Failed = 3
}

// ============ REQUEST TYPES ============

interface CreateOrderRequest {
  fullName: string;
  phoneNumber: string;
  address: string;
  notes?: string;
  paymentMethod: EPaymentMethod;
  saveDeliveryProfile?: boolean;
}

interface CreateCheckoutSessionRequest {
  orderId: string;
}

// ============ RESPONSE TYPES ============

interface OrderResponse {
  id: string;
  orderNumber: string;
  status: EOrderStatus;
  paymentMethod: EPaymentMethod;
  paymentStatus: EPaymentStatus;
  total: number;
  currency: string;
  items: OrderItem[];
  deliveryInfo: DeliveryInfo;
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
  subtotal: number;
}

interface DeliveryInfo {
  fullName: string;
  phoneNumber: string;
  address: string;
  notes?: string;
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
  payments: Payment[];
  refunds: Refund[];
}

interface Payment {
  id: string;
  sessionId: string;
  status: string;
  amount: number;
  createdAt: string;
}

interface Refund {
  id: string;
  amount: number;
  status: ERefundStatus;
  reason?: string;
  createdAt: string;
}

interface OrderDetail extends OrderResponse {
  stripePaymentIntentId?: string;
  stripeChargeId?: string;
  payment?: Payment;
  refunds?: Refund[];
  confirmedAt?: string;
  deliveredAt?: string;
}
```

---

## 🚀 Key Points for Frontend Implementation

### **Must Remember**

✅ **Stripe orders are created WITHOUT payment** - Payment comes later via webhook  
✅ **COD orders are created WITH payment confirmed** - NotRequired status is immediate  
✅ **Order.Status and Payment.Status are separate** - One is fulfillment, one is payment  
✅ **Stripe orders require Pending → Paid before confirming** - Cannot skip payment  
✅ **Webhook timing is async** - Polling is needed after Stripe redirect  
✅ **Refunds are async** - Webhook confirms settlement  

### **Common Mistakes to Avoid**

❌ Creating order BEFORE payment for Stripe (async is important)  
❌ Assuming cart clears immediately (clears after payment confirmation)  
❌ Skipping payment status checks (especially for Stripe)  
❌ Treating payment status same as order status (they're independent)  
❌ Not handling webhook delays (poll status after Stripe redirect)  
❌ Not validating phone numbers properly (Vietnam-specific format)  

---

## 📞 Reference

- **API Docs**: README.md
- **Payment Guide**: FRONTEND_DELIVERY_GUIDE.md
- **Architecture**: PAYMENT_ARCHITECTURE_REVIEW.md
- **Stripe Docs**: https://stripe.com/docs

---

**Document prepared for**: Frontend Development Team  
**Ready for**: Implementation (May 20-24, 2026)  
**Backend Status**: ✅ Production-ready (29/29 tests passing)
