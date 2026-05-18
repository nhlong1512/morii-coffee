# Order, Checkout & Payment - Complete Flow Diagram

**Purpose**: Visualize the entire flow from Client → Frontend → Backend → Stripe → Database  
**Covers**: Both COD and Stripe payment methods  
**Audience**: Entire team (Frontend, Backend, DevOps)

---

## 📊 High-Level Architecture

```
┌──────────────────┐
│     CLIENT       │ (Customer's browser)
│   (User/Browser) │
└────────┬─────────┘
         │
         ▼
┌──────────────────────────────────┐
│        FRONTEND (React)           │ (Next.js App)
│  - Cart management               │
│  - Checkout UI                   │
│  - Payment method selection      │
│  - Stripe integration            │
└────────┬─────────────────────────┘
         │
         ▼ (HTTP/REST API)
┌──────────────────────────────────┐
│     BACKEND (.NET)               │ (Clean Architecture)
│  - Order creation                │
│  - Payment processing            │
│  - Webhook handling              │
│  - Business logic                │
└────────┬──────────────┬──────────┘
         │              │
         ▼              ▼
    ┌────────┐    ┌──────────┐
    │DATABASE│    │  STRIPE  │
    │(PgSQL) │    │(3rd party)
    └────────┘    └──────────┘
```

---

## 🛒 FULL FLOW: COD (Cash on Delivery)

### **Timeline: Client → Frontend → Backend → Database**

```
┌─────────────────────────────────────────────────────────────────┐
│ STEP 1: USER ADDS ITEMS TO CART (No backend call)              │
└─────────────────────────────────────────────────────────────────┘

CLIENT (Browser)
  │
  ├─ User: Clicks "Add to Cart" button
  │
  └─► FRONTEND (React)
       │
       ├─ Store item in Redux/Context state
       ├─ Store in localStorage (guest cart)
       ├─ Update cart UI (show item + quantity)
       │
       └─ No backend call yet ✓


┌─────────────────────────────────────────────────────────────────┐
│ STEP 2: USER PROCEEDS TO CHECKOUT                              │
└─────────────────────────────────────────────────────────────────┘

CLIENT
  │
  └─► FRONTEND
       │
       ├─ Show checkout form:
       │  - Full name
       │  - Phone number
       │  - Address
       │  - Notes (optional)
       │  - Payment method: [ ] COD  [ ] Card
       │
       └─ User selects: COD


┌─────────────────────────────────────────────────────────────────┐
│ STEP 3: SUBMIT CHECKOUT (First backend call)                   │
└─────────────────────────────────────────────────────────────────┘

CLIENT
  │
  └─► FRONTEND
       │
       ├─ POST /api/v1/orders
       │  {
       │    "fullName": "Nguyễn Văn A",
       │    "phoneNumber": "0775504619",
       │    "address": "1170/61 3 Tháng 2, Q.11, HCM",
       │    "notes": "No ice",
       │    "paymentMethod": 1,  // EPaymentMethod.COD
       │    "saveDeliveryProfile": true
       │  }
       │
       └─► BACKEND (.NET)
            │
            ├─ Validate delivery info
            ├─ Validate phone number (regex: Vietnam format)
            ├─ Validate cart is not empty
            ├─ Validate user doesn't have pending COD order
            │
            ├─► DATABASE (PostgreSQL)
            │    │
            │    ├─ INSERT INTO Orders (
            │    │    id = new UUID
            │    │    userId = {authenticated user}
            │    │    orderNumber = "MRC-20260518-001"
            │    │    status = PENDING
            │    │    paymentMethod = COD (1)
            │    │    paymentStatus = NotRequired (1) ← KEY: COD = No payment needed
            │    │    total = 250000
            │    │    currency = "vnd"
            │    │    createdAt = now
            │    │  )
            │    │
            │    ├─ INSERT INTO OrderItems (...)
            │    │    For each item in cart
            │    │
            │    ├─ INSERT INTO DeliveryInfo (...)
            │    │
            │    └─ DELETE FROM Cart (clear user's cart) ✓
            │
            └─ Return response:
               {
                 "id": "abc-123",
                 "orderNumber": "MRC-20260518-001",
                 "status": "PENDING",
                 "paymentStatus": "NotRequired",  ← Paid immediately
                 "paymentMethod": "COD",
                 "total": 250000,
                 "items": [...]
               }


┌─────────────────────────────────────────────────────────────────┐
│ STEP 4: SHOW SUCCESS PAGE (Immediate)                          │
└─────────────────────────────────────────────────────────────────┘

CLIENT
  │
  └─► FRONTEND
       │
       ├─ Receive order response with paymentStatus = "NotRequired"
       │
       ├─ Show success page:
       │  ┌─────────────────────────────────┐
       │  │ ✅ Order Confirmed              │
       │  │                                 │
       │  │ Order #: MRC-20260518-001       │
       │  │ Total: 250,000 VND              │
       │  │ Payment: Pay on Delivery ✓      │
       │  │                                 │
       │  │ [View Order] [Continue Shopping]│
       │  └─────────────────────────────────┘
       │
       └─ Store order ID in localStorage for reference


┌─────────────────────────────────────────────────────────────────┐
│ STEP 5: STAFF PREPARES ORDER (Async background)                │
└─────────────────────────────────────────────────────────────────┘

BACKEND (Hangfire Job / Manual)
  │
  ├─► DATABASE
  │    │
  │    └─ UPDATE Orders SET status = CONFIRMED WHERE id = "abc-123"
  │       (Staff confirms order is feasible)
  │
  └─ Order moves through fulfillment:
     PENDING → CONFIRMED → READY_TO_PICKUP → IN_DELIVERY → DELIVERED


┌─────────────────────────────────────────────────────────────────┐
│ STEP 6: CUSTOMER RECEIVES ORDER (Offline)                      │
└─────────────────────────────────────────────────────────────────┘

CLIENT (In-store / At home)
  │
  └─ Pays 250,000 VND in cash
```

---

## 💳 FULL FLOW: STRIPE (Online Card Payment)

### **Timeline: Client → Frontend → Backend → Stripe → Database**

```
┌─────────────────────────────────────────────────────────────────┐
│ STEP 1: USER ADDS ITEMS TO CART (Same as COD)                  │
└─────────────────────────────────────────────────────────────────┘

[Same as COD Step 1]


┌─────────────────────────────────────────────────────────────────┐
│ STEP 2: USER PROCEEDS TO CHECKOUT (Same as COD)                │
└─────────────────────────────────────────────────────────────────┘

[Same as COD Step 2, but selects: Stripe]


┌─────────────────────────────────────────────────────────────────┐
│ STEP 3: CREATE ORDER WITHOUT PAYMENT (Different from COD)      │
└─────────────────────────────────────────────────────────────────┘

CLIENT
  │
  └─► FRONTEND
       │
       ├─ POST /api/v1/orders
       │  {
       │    "fullName": "Nguyễn Văn A",
       │    "phoneNumber": "0775504619",
       │    "address": "1170/61 3 Tháng 2, Q.11, HCM",
       │    "notes": "No ice",
       │    "paymentMethod": 4,  // EPaymentMethod.STRIPE
       │    "saveDeliveryProfile": true
       │  }
       │
       └─► BACKEND (.NET)
            │
            ├─ Validate delivery info
            ├─ Validate phone number
            ├─ Validate cart is not empty
            │
            ├─► DATABASE (PostgreSQL)
            │    │
            │    └─ INSERT INTO Orders (
            │         id = new UUID ("abc-stripe-123")
            │         userId = {authenticated user}
            │         orderNumber = "MRC-20260518-002"
            │         status = PENDING
            │         paymentMethod = STRIPE (4)
            │         paymentStatus = Pending (2) ← KEY: Waiting for payment
            │         total = 250000
            │         createdAt = now
            │       )
            │
            └─ Return response:
               {
                 "id": "abc-stripe-123",
                 "orderNumber": "MRC-20260518-002",
                 "status": "PENDING",
                 "paymentStatus": "Pending",  ← NOT paid yet
                 "paymentMethod": "STRIPE",
                 "total": 250000
               }


┌─────────────────────────────────────────────────────────────────┐
│ STEP 4: CREATE STRIPE CHECKOUT SESSION                         │
└─────────────────────────────────────────────────────────────────┘

CLIENT
  │
  └─► FRONTEND
       │
       ├─ POST /api/v1/payments/stripe/checkout-session
       │  {
       │    "orderId": "abc-stripe-123"
       │  }
       │
       └─► BACKEND (.NET)
            │
            ├─ Validate order exists
            ├─ Validate order.paymentStatus = Pending
            ├─ Validate order.paymentMethod = STRIPE
            │
            ├─► STRIPE (API Call)
            │    │
            │    ├─ Create Checkout Session
            │    │  {
            │    │    "client_reference_id": "abc-stripe-123",
            │    │    "line_items": [
            │    │      {
            │    │        "price_data": {
            │    │          "currency": "vnd",
            │    │          "unit_amount": 250000,
            │    │          "product_data": {
            │    │            "name": "Order #MRC-20260518-002"
            │    │          }
            │    │        },
            │    │        "quantity": 1
            │    │      }
            │    │    ],
            │    │    "mode": "payment",
            │    │    "success_url": "https://yourapp.com/checkout/success?session_id={CHECKOUT_SESSION_ID}",
            │    │    "cancel_url": "https://yourapp.com/checkout/cancel",
            │    │    "metadata": {
            │    │      "orderId": "abc-stripe-123",
            │    │      "userId": "user-id"
            │    │    }
            │    │  }
            │    │
            │    └─ Response:
            │       {
            │         "id": "cs_test_abcd1234",
            │         "url": "https://checkout.stripe.com/c/pay/cs_test_abcd1234",
            │         "expires_at": 1716144000 (24 hours from now)
            │       }
            │
            └─ Return to Frontend:
               {
                 "checkoutUrl": "https://checkout.stripe.com/c/pay/cs_test_abcd1234",
                 "sessionId": "cs_test_abcd1234",
                 "amount": 250000,
                 "currency": "vnd"
               }


┌─────────────────────────────────────────────────────────────────┐
│ STEP 5: REDIRECT TO STRIPE CHECKOUT                            │
└─────────────────────────────────────────────────────────────────┘

CLIENT
  │
  └─► FRONTEND
       │
       ├─ Receive checkoutUrl
       │
       └─ window.location.href = checkoutUrl
          │
          └─ Browser redirects to: https://checkout.stripe.com/c/pay/cs_test_abcd1234
             │
             └─► STRIPE (Customer enters card details)
                  │
                  ├─ Stripe-hosted checkout page (PCI-DSS compliant)
                  │  ┌──────────────────────────────┐
                  │  │ Stripe Checkout              │
                  │  │                              │
                  │  │ Order: MRC-20260518-002      │
                  │  │ Total: 250,000 VND           │
                  │  │                              │
                  │  │ Card number: [______________]│
                  │  │ MM/YY: [____]  CVC: [___]   │
                  │  │                              │
                  │  │ [Pay 250,000 VND]            │
                  │  └──────────────────────────────┘
                  │
                  ├─ User enters card: 4242 4242 4242 4242
                  ├─ User confirms payment
                  │
                  └─ Stripe processes payment
                     │
                     ├─ Payment succeeds ✅
                     │  OR
                     └─ Payment fails ❌


┌─────────────────────────────────────────────────────────────────┐
│ STEP 6A: PAYMENT SUCCEEDS (Happy Path)                         │
└─────────────────────────────────────────────────────────────────┘

STRIPE
  │
  ├─ Create PaymentIntent: pi_test_paid
  ├─ Create Charge: ch_test_paid
  ├─ Mark session as completed
  │
  └─► Send Webhook to Backend
       POST /api/v1/payments/stripe/webhook
       {
         "id": "evt_1Jv4dUH43V0hEKQN4T0zVW4T",
         "type": "checkout.session.completed",
         "data": {
           "object": {
             "id": "cs_test_abcd1234",
             "client_reference_id": "abc-stripe-123",
             "payment_intent": "pi_test_paid",
             "status": "complete",
             "payment_status": "paid"
           }
         }
       }
       Header: Stripe-Signature: {HMAC-SHA256}


BACKEND (.NET) - Webhook Handler
  │
  ├─ Verify Stripe signature (HMAC-SHA256) ✓
  ├─ Check if event already processed (idempotency via UNIQUE on StripeEventId)
  │
  ├─► DATABASE
  │    │
  │    ├─ INSERT INTO PaymentWebhookEvents (
  │    │    eventId = "evt_1Jv4dUKN4T0zVW4T",
  │    │    eventType = "checkout.session.completed",
  │    │    stripeEventId = "evt_1Jv4dUKN4T0zVW4T"
  │    │  ) ← UNIQUE constraint prevents duplicate processing
  │    │
  │    ├─ INSERT INTO Payments (
  │    │    orderId = "abc-stripe-123",
  │    │    sessionId = "cs_test_abcd1234",
  │    │    paymentIntentId = "pi_test_paid",
  │    │    chargeId = "ch_test_paid",
  │    │    status = "Succeeded",
  │    │    amount = 250000,
  │    │    paidAt = now
  │    │  )
  │    │
  │    └─ UPDATE Orders SET
  │         status = ??? (depends on business logic, maybe PENDING still?)
  │         paymentStatus = Paid (3)
  │       WHERE id = "abc-stripe-123"
  │
  └─ Webhook response: 200 OK
     {
       "received": true,
       "result": "Processed"
     }


CLIENT (Browser waiting at Stripe)
  │
  ├─ Payment completed successfully
  │
  └─► Stripe redirects to:
      https://yourapp.com/checkout/success?session_id=cs_test_abcd1234


FRONTEND (Success Page)
  │
  ├─ Extract session_id from URL
  ├─ Poll backend for order status (3 second intervals)
  │
  ├─ GET /api/v1/payments/by-order/abc-stripe-123
  │
  └─► BACKEND
       │
       ├─► DATABASE
       │    │
       │    └─ SELECT * FROM Orders WHERE id = "abc-stripe-123"
       │       (Should now have paymentStatus = Paid)
       │
       └─ Return:
          {
            "orderId": "abc-stripe-123",
            "paymentStatus": "Paid",
            "orderStatus": "PENDING",
            "payments": [
              {
                "id": "pay-123",
                "sessionId": "cs_test_abcd1234",
                "status": "Succeeded",
                "amount": 250000,
                "createdAt": "2026-05-18T10:30:00Z"
              }
            ],
            "refunds": []
          }


FRONTEND (Display Success)
  │
  ├─ Show success page:
  │  ┌────────────────────────────────────┐
  │  │ ✅ Payment Successful              │
  │  │                                    │
  │  │ Order #: MRC-20260518-002          │
  │  │ Total: 250,000 VND (Paid) ✓       │
  │  │ Status: Confirming...              │
  │  │                                    │
  │  │ [View Order] [Continue Shopping]   │
  │  └────────────────────────────────────┘
  │
  └─ Store orderId in localStorage


┌─────────────────────────────────────────────────────────────────┐
│ STEP 6B: PAYMENT FAILS (Failure Path)                          │
└─────────────────────────────────────────────────────────────────┘

STRIPE
  │
  ├─ Card is declined (e.g., insufficient funds)
  │
  └─► Stripe redirects to:
      https://yourapp.com/checkout/cancel


FRONTEND (Failure Page)
  │
  ├─ GET /api/v1/payments/by-order/abc-stripe-123
  │
  └─► BACKEND
       │
       ├─► DATABASE
       │    │
       │    └─ SELECT * FROM Orders WHERE id = "abc-stripe-123"
       │       (Should have paymentStatus = Failed)
       │
       └─ Return:
          {
            "orderId": "abc-stripe-123",
            "paymentStatus": "Failed",
            "orderStatus": "PENDING",
            "payments": []
          }


FRONTEND (Display Failure)
  │
  ├─ Show failure page:
  │  ┌────────────────────────────────────┐
  │  │ ❌ Payment Failed                  │
  │  │                                    │
  │  │ Your card was declined             │
  │  │ Order #: MRC-20260518-002          │
  │  │ Total: 250,000 VND                 │
  │  │                                    │
  │  │ [Retry] [Use Different Card]       │
  │  │ [Cancel Order]                     │
  │  └────────────────────────────────────┘
  │
  └─ User can retry or cancel


┌─────────────────────────────────────────────────────────────────┐
│ STEP 7: REFUND FLOW (Admin only, Async)                        │
└─────────────────────────────────────────────────────────────────┘

ADMIN (Backend UI)
  │
  ├─ POST /api/v1/payments/abc-stripe-123/refund
  │  {
  │    "amount": 100000,  // Partial refund (out of 250000)
  │    "reason": "Customer requested partial refund"
  │  }
  │
  └─► BACKEND (.NET)
       │
       ├─ Validate order paymentStatus = Paid
       ├─ Validate refund amount ≤ remaining balance
       │
       ├─► STRIPE (API Call)
       │    │
       │    └─ Create Refund
       │       {
       │         "charge": "ch_test_paid",
       │         "amount": 100000,
       │         "metadata": {
       │           "orderId": "abc-stripe-123",
       │           "reason": "Customer requested partial refund"
       │         }
       │       }
       │
       ├─► DATABASE
       │    │
       │    └─ INSERT INTO Refunds (
       │         paymentId = "pay-123",
       │         stripeRefundId = "re_test_partial",
       │         amount = 100000,
       │         status = "Initiated"
       │       )
       │    │
       │    └─ UPDATE Orders SET
       │         paymentStatus = PartiallyRefunded
       │       WHERE id = "abc-stripe-123"
       │
       └─ Return to Admin:
          {
            "refundId": "re_test_partial",
            "status": "Initiated",
            "amount": 100000
          }


STRIPE (Webhook for Refund)
  │
  ├─ Process refund
  ├─ Send webhook: charge.refunded
  │
  └─► Backend receives:
      POST /api/v1/payments/stripe/webhook
      {
        "type": "charge.refunded",
        "data": {
          "object": {
            "id": "ch_test_paid",
            "refunded": true,
            "refunds": {
              "data": [
                {
                  "id": "re_test_partial",
                  "amount": 100000
                }
              ]
            }
          }
        }
      }


BACKEND (Webhook Handler)
  │
  ├─► DATABASE
  │    │
  │    └─ UPDATE Refunds SET
  │         status = "Settled"
  │       WHERE stripeRefundId = "re_test_partial"
  │
  └─ Webhook response: 200 OK
```

---

## 🔄 Parallel Requests & Async Processing

### **Request Timeline (Stripe Payment)**

```
Time   Client          Frontend         Backend          Stripe       Database
────   ──────          ────────         ───────          ──────       ────────
0s     [Add Items]
       └─► Redux/Ctx
            (no call)

10s    [Checkout]
       └─► Form Show

15s                    POST /orders
                       │                POST /insert
                       │                Order row
                       │                ────────────► INSERT ✓
                       │                ◄───────────
                       ◄─── Response

20s                    POST /checkout-session
                       │                ────────────► Create
                       │                ◄────────────  Session
                       │                (cs_test_...)
                       ◄─── Response

25s    Redirect to Stripe ───────────────►
       (User enters card)

35s    [Card confirmed]                   POST
       ◄─────────────────────────────────►
                                          PaymentIntent

36s    Redirect to                        (Async Webhook)
       /checkout/success                  ────────────► INSERT
                                          Payment row
                                          UPDATE
                                          Order.paymentStatus
                                          ────────────► Done ✓

37s                    Poll /by-order
                       │                SELECT ◄────────
                       │                paymentStatus
                       ◄─── Paid ✓

40s    Show Success
```

---

## 📋 Summary Table: COD vs STRIPE

| Step | COD | STRIPE |
|------|-----|--------|
| **1. Add to Cart** | Local state | Local state |
| **2. Checkout Form** | Show delivery form | Show delivery form |
| **3. Create Order** | `POST /orders` | `POST /orders` |
| **4. Order Status** | paymentStatus = NotRequired | paymentStatus = Pending |
| **5. Cart Clear** | Cleared immediately | Cleared immediately |
| **6. Show Page** | Success page (instant) | Waiting page (⏳) |
| **7. Stripe Session** | ❌ Not needed | `POST /checkout-session` |
| **8. Redirect** | ❌ Nowhere | Redirect to Stripe |
| **9. Payment** | User pays in-store | Stripe processes online |
| **10. Webhook** | ❌ Not used | Stripe sends webhook |
| **11. Order Update** | Already confirmed | Updated after webhook |
| **12. Status** | PENDING → CONFIRMED | PENDING → CONFIRMED |

---

## 🎯 Key Business Logic Points

### **Order Creation**
```
Input: User clicks "Confirm"
├─ Cart items
├─ Delivery info
└─ Payment method

Process:
├─ Validate cart is not empty
├─ Validate delivery info
├─ Validate phone number (regex: Vietnam)
├─ Check for duplicate pending orders
└─ Create order in database

Output:
├─ Order ID
├─ Payment status (NotRequired for COD, Pending for Stripe)
└─ Order status (PENDING)

Side effects:
└─ Clear cart (localStorage + database if needed)
```

### **Stripe Session Creation**
```
Input: Order ID
├─ Order must exist
├─ Order must have paymentMethod = STRIPE
└─ Order must have paymentStatus = Pending

Process:
├─ Call Stripe API (CreateCheckoutSession)
├─ Pass order total + metadata
└─ Receive session URL

Output:
├─ Checkout URL (Stripe-hosted)
├─ Session ID
└─ Publishable key (optional)

Side effects:
└─ Stripe creates session internally
```

### **Webhook Processing**
```
Input: Stripe webhook event
├─ Event type (checkout.session.completed, charge.refunded, etc.)
├─ Stripe signature header
└─ Raw body

Process:
├─ Verify signature (HMAC-SHA256)
├─ Check for duplicate (UNIQUE constraint on eventId)
├─ Parse event data
├─ Update order/payment status
└─ Store audit record

Output:
├─ 200 OK (if valid)
└─ 422 Unprocessable (if signature invalid)

Side effects:
├─ Update database
├─ Update order status
└─ Audit trail
```

---

## 🔐 Security Points

```
┌─────────────────────────────────────────────────┐
│ SECURITY CHECKPOINTS                            │
└─────────────────────────────────────────────────┘

1. Order Creation
   ✓ User authentication (JWT)
   ✓ User owns order (userId check)
   ✓ Phone number validation (regex)
   ✓ Address validation (length check)

2. Checkout Session
   ✓ Order exists
   ✓ Order belongs to user
   ✓ Order payment status is Pending

3. Webhook Handling
   ✓ Stripe signature verification (HMAC-SHA256)
   ✓ Idempotency (UNIQUE constraint on eventId)
   ✓ Audit logging (all webhooks recorded)

4. Refund
   ✓ Admin-only (authorization check)
   ✓ Order exists
   ✓ Payment status allows refund
   ✓ Refund amount validation

5. Card Data
   ✓ NEVER touches backend
   ✓ Stripe-hosted checkout (PCI-DSS compliant)
   ✓ Only session IDs stored in database
```

---

## 💾 Database State at Each Step

### **For COD Order:**

```
Step 1: After POST /orders
┌─────────────┬──────────┬─────────────────┐
│ Table       │ Action   │ Result          │
├─────────────┼──────────┼─────────────────┤
│ Orders      │ INSERT   │ id, status=PENDING
│             │          │ paymentStatus=NotRequired
│             │          │ paymentMethod=COD
│             │          │ total=250000
├─────────────┼──────────┼─────────────────┤
│ OrderItems  │ INSERT   │ item1, item2, ...
├─────────────┼──────────┼─────────────────┤
│ DeliveryInfo│ INSERT   │ fullName, phone, address
├─────────────┼──────────┼─────────────────┤
│ Cart        │ DELETE   │ (for this user)
└─────────────┴──────────┴─────────────────┘

Final state: Order ready, no payment record needed ✓
```

### **For Stripe Order (Before Payment):**

```
Step 1: After POST /orders
┌─────────────┬──────────┬─────────────────┐
│ Table       │ Action   │ Result          │
├─────────────┼──────────┼─────────────────┤
│ Orders      │ INSERT   │ id, status=PENDING
│             │          │ paymentStatus=Pending
│             │          │ paymentMethod=STRIPE
│             │          │ total=250000
├─────────────┼──────────┼─────────────────┤
│ OrderItems  │ INSERT   │ item1, item2, ...
├─────────────┼──────────┼─────────────────┤
│ DeliveryInfo│ INSERT   │ fullName, phone, address
├─────────────┼──────────┼─────────────────┤
│ Cart        │ DELETE   │ (for this user)
├─────────────┼──────────┼─────────────────┤
│ Payments    │ NOTHING  │ (wait for webhook)
└─────────────┴──────────┴─────────────────┘

Step 2: After webhook (payment successful)
┌─────────────┬──────────┬─────────────────┐
│ Table       │ Action   │ Result          │
├─────────────┼──────────┼─────────────────┤
│ Orders      │ UPDATE   │ paymentStatus=Paid
├─────────────┼──────────┼─────────────────┤
│ Payments    │ INSERT   │ id, sessionId, status
│             │          │ paymentIntentId, chargeId
├─────────────┼──────────┼─────────────────┤
│ PaymentWebh │ INSERT   │ eventId, eventType
│ ookEvents   │          │ (idempotency)
└─────────────┴──────────┴─────────────────┘

Final state: Order ready with payment confirmed ✓
```

---

## 📞 Quick Reference

### **Frontend Should Know**

✅ **For COD**: Order created immediately, no Stripe call needed  
✅ **For Stripe**: Order created, then create checkout session, then redirect  
✅ **Webhook is async**: May not be instant, poll status after redirect  
✅ **Cart clears immediately**: Before payment confirmation (intentional)  
✅ **Refunds are async**: Admin initiates, Stripe webhook confirms  

### **Database Should Know**

✅ **UNIQUE(StripeEventId)**: Prevents duplicate webhook processing  
✅ **Foreign keys**: Orders → OrderItems, Orders → DeliveryInfo  
✅ **Indexes needed**: On userId, orderId, sessionId, eventId  
✅ **No card data**: Only session/payment/charge IDs stored  

### **Backend Should Know**

✅ **Signature verification**: HMAC-SHA256 before processing webhook  
✅ **Idempotency**: Check if eventId already processed  
✅ **Error handling**: Retry logic for failed API calls  
✅ **Logging**: All webhook events logged for audit  

---

Generated for: Full team understanding  
Date: May 18, 2026  
Status: ✅ Production Ready
