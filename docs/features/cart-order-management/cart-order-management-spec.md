# Cart & Order Management — Feature Description

**Feature ID:** `cart-order-management`  
**Branch:** `009-cart-payment`  
**Status:** In Design — UI Phase  
**Last Updated:** 2026-04-26

---

## Table of Contents

1. [Overview](#1-overview)
2. [Business Context](#2-business-context)
3. [User Journeys](#3-user-journeys)
4. [Page Specifications](#4-page-specifications)
   - 4.1 [Cart Page](#41-cart-page)
   - 4.2 [Checkout Page](#42-checkout-page)
   - 4.3 [Order History Page](#43-order-history-page)
   - 4.4 [Order Detail Page](#44-order-detail-page)
5. [Data Model](#5-data-model)
6. [Order Status Lifecycle](#6-order-status-lifecycle)
7. [Payment Methods](#7-payment-methods)
8. [Delivery Information](#8-delivery-information)
9. [Pricing & Calculation Logic](#9-pricing--calculation-logic)
10. [Cart Storage Strategy](#10-cart-storage-strategy)
11. [API Contracts](#11-api-contracts)
12. [Admin Responsibilities](#12-admin-responsibilities)
13. [Auto-Complete Mechanism](#13-auto-complete-mechanism)
14. [UI/UX Recommendations](#14-uiux-recommendations)
15. [Future Enhancements](#15-future-enhancements)
16. [Out of Scope (Current Phase)](#16-out-of-scope-current-phase)

---

## 1. Overview

Cart & Order Management is the core transactional feature for Morii Coffee's online platform. It covers the full lifecycle from a customer adding items to their cart, through checkout, order placement, delivery tracking, to final order review.

**Service Model:** Delivery-only. Morii Coffee does not offer dine-in service. All orders are shipped to the customer's address by in-house staff.

---

## 2. Business Context

- **Scale:** Small operation — Morii staff handle their own deliveries (no 3rd-party logistics integration at this phase).
- **Order Status Management:** Staff manually update order status via the Admin Panel. No customer-triggered automation beyond placing the order.
- **Auto-Completion:** Orders stuck in `IN_DELIVERY` status automatically transition to `DELIVERED` after a configured number of days (configurable, default: 3 days), mimicking industry patterns (e.g. Shopee's 7-day auto-complete).
- **Phase Scope:** This phase focuses on UI + COD payment. Momo and PayPal integration are deferred.

---

## 3. User Journeys

### 3.1 Primary Flow — Place an Order

```
Product Page
  → Add to Cart (select quantity, size/variant)
  → Cart Page (review items, see price summary)
  → Checkout Page (fill delivery info, choose payment method)
  → [Submit Order] → API call to create order
  → Redirect to Order History Page
```

### 3.2 Return User Flow

```
Checkout Page
  → Delivery info auto-filled from saved profile
  → Confirm or edit → [Đặt Giao]
```

### 3.3 Order Tracking Flow

```
Order History Page
  → Select an order
  → Order Detail Page (status timeline, item breakdown, price summary)
```

---

## 4. Page Specifications

### 4.1 Cart Page

**Route:** `/cart`

#### Layout

Two-column layout (desktop), stacked (mobile):
- **Left / Main:** List of cart items
- **Right / Sidebar:** Price summary + CTA

#### Cart Item Component

Each item in the cart displays:
- Product thumbnail image
- Product name
- Variant/Size label (e.g. "Size M", "Size L")
- Unit price
- Quantity stepper (`-` / `+` buttons, or direct input)
- Line total (unit price × quantity)
- Remove item button (trash icon)

**Edge cases:**
- Empty cart: show empty state illustration + "Khám phá thực đơn" CTA button back to menu
- If a product is no longer available (soft-deleted), show a warning inline and disable checkout

#### Price Summary Sidebar

| Label | Value |
|---|---|
| Tạm tính (Subtotal) | Sum of all line totals |
| Thuế (Tax) | % applied to subtotal |
| Phí vận chuyển (Shipping) | Flat fee or distance-based (TBD) |
| Khuyến mãi (Discount) | Applied coupon/promo deduction |
| **Tổng cộng (Total)** | **Subtotal + Tax + Shipping − Discount** |

- Discount row is hidden when no promotion is applied
- Shipping fee displays "Miễn phí" (Free) when applicable, otherwise shows the fee amount

#### CTA

- Button: **"Thanh toán"** — navigates to `/checkout`
- Disabled when cart is empty

---

### 4.2 Checkout Page

**Route:** `/checkout`

#### Layout

Two-column layout (mirrors Cart page):
- **Left / Main:** Delivery Info form + Payment Method selector
- **Right / Sidebar:** Price summary (same component as Cart page, read-only)

#### Section A — Delivery Information Form

| Field | Type | Validation |
|---|---|---|
| Họ và tên (Full Name) | Text input | Required, min 2 chars |
| Số điện thoại (Phone Number) | Tel input | Required, Vietnamese phone format |
| Địa chỉ giao hàng (Delivery Address) | Text area | Required, min 10 chars |

- If user has a saved delivery profile, fields are pre-populated
- Checkbox: **"Lưu thông tin giao hàng"** — saves the entered info to the user's profile for future use
- If saved info exists, show a "Sử dụng thông tin đã lưu" quick-fill link

#### Section B — Payment Method

Radio button group (COD is selected by default):

| Option | Icon | Label | Description |
|---|---|---|---|
| `COD` | Cash/wallet icon | Thanh toán khi nhận hàng | Pay cash on delivery |
| `MOMO` | MoMo branded icon | Ví MoMo | Mobile wallet (integration deferred) |
| `PAYPAL` | PayPal branded icon | PayPal | International payment (integration deferred) |

- MoMo and PayPal options are visible but show a badge: **"Sắp ra mắt"** (Coming Soon) or are selectable but trigger a "Tính năng đang phát triển" toast on submit — TBD with product.
- Use proper branded icons for each payment method (not generic icons).

#### Section C — Order Notes (Optional Enhancement)

- Optional text area: **"Ghi chú đơn hàng"** (Order notes for the delivery staff)
- Example placeholder: "Giao trước 10 giờ sáng, gọi trước khi đến"

#### CTA

- Button: **"Đặt Giao"** — submits the order
- Shows loading state while API is in-flight
- On success: redirect to `/orders` (Order History)
- On error: display inline error toast, remain on page

---

### 4.3 Order History Page

**Route:** `/orders`

#### Layout

Full-width list of orders, sorted by `createdAt` descending.

#### Order Card

Each order card displays:
- Order ID (e.g. `MRC-20250310-003`)
- Order date & time
- Status badge (color-coded by status)
- Item count + first product name (e.g. "Cà phê sữa và 2 món khác")
- Total amount
- CTA: **"Xem chi tiết"** → navigates to `/orders/{orderId}`

#### States

- Loading skeleton while fetching
- Empty state: "Bạn chưa có đơn hàng nào" + CTA to menu

---

### 4.4 Order Detail Page

**Route:** `/orders/:orderId`

#### Content

- Order ID + placed date
- **Status Timeline** — visual step indicator showing all statuses, current one highlighted
- Delivery Info summary (name, phone, address)
- Item list (same as cart item display, read-only, no quantity controls)
- Price summary breakdown (same as checkout sidebar)
- Payment method used
- Order notes (if any)

---

## 5. Data Model

### Order Entity

```
Order {
  id:            string          // Format: MRC-YYYYMMDD-NNN (e.g. MRC-20250310-003)
  userId:        Guid
  
  products: [
    {
      // Extends ProductDto snapshot (frozen at order time)
      productId:     Guid
      productName:   string
      variantId:     Guid?
      variantLabel:  string?      // e.g. "Size M"
      unitPrice:     decimal
      quantity:      int
      lineTotal:     decimal      // unitPrice * quantity
    }
  ]
  
  deliveryInfo: {
    fullName:      string
    phoneNumber:   string
    address:       string
  }
  
  notes:          string?         // Optional order note
  
  paymentMethod:  enum            // COD | MOMO | PAYPAL
  
  subtotal:       decimal         // Sum of all lineTotals
  tax:            decimal         // Tax amount (not rate)
  shipping:       decimal         // Shipping fee
  discount:       decimal         // Discount/promo deduction
  total:          decimal         // subtotal + tax + shipping - discount
  
  orderStatus:    enum            // See Section 6
  
  createdAt:      datetime
  updatedAt:      datetime
  createdBy:      string
  updatedBy:      string
}
```

**Key design decisions:**
- Products are **snapshotted** at order time (name, price, variant). This ensures historical orders remain accurate even if products are later modified or deleted.
- `id` uses a human-readable format (`MRC-YYYYMMDD-NNN`) for staff readability and customer support. The sequential suffix `NNN` resets per day.
- `tax`, `shipping`, and `discount` are stored as absolute amounts (not percentages) so that order totals are immutable after placement.

### Saved Delivery Info (User Profile Extension)

```
UserDeliveryProfile {
  userId:        Guid
  fullName:      string
  phoneNumber:   string
  address:       string
  updatedAt:     datetime
}
```

One record per user (upsert on save). Used to pre-fill the checkout form.

---

## 6. Order Status Lifecycle

```
PENDING
  → Staff reviews order
CONFIRMED
  → Staff confirms order, begins preparation
READY_TO_PICKUP
  → Order is packaged, awaiting staff pickup for delivery
IN_DELIVERY
  → Staff is en route to customer
DELIVERED
  → Order successfully received  ← (can be set manually by staff, or auto-completed)
REVIEWED
  → Customer has submitted a review for the order
```

### Status Descriptions

| Status | Vietnamese Label | Trigger |
|---|---|---|
| `PENDING` | Đơn hàng đã đặt | Customer places order |
| `CONFIRMED` | Đã xác nhận | Admin confirms |
| `READY_TO_PICKUP` | Chờ lấy hàng | Admin marks ready |
| `IN_DELIVERY` | Đang giao | Admin marks in delivery |
| `DELIVERED` | Đã giao thành công | Admin marks delivered OR auto-complete |
| `REVIEWED` | Đã đánh giá | Customer submits review (future feature) |

### Cancellation

- Customers may cancel an order only while it is in `PENDING` status.
- After `CONFIRMED`, cancellation requires admin action.
- A `CANCELLED` terminal status should be included in the enum.

---

## 7. Payment Methods

| Method | Enum Value | Phase 1 | Notes |
|---|---|---|---|
| Cash on Delivery | `COD` | ✅ Fully supported | Default selection |
| MoMo Wallet | `MOMO` | ⏳ UI only | Integration deferred |
| PayPal | `PAYPAL` | ⏳ UI only | Integration deferred |

**Icon requirements:**
- COD: custom cash/wallet icon (not a generic dollar sign)
- MoMo: official MoMo pink logo
- PayPal: official PayPal blue logo
- Icons must meet brand guidelines for MoMo and PayPal if used publicly

---

## 8. Delivery Information

### Form Behavior

1. On page load, call `GET /api/users/me/delivery-profile`
2. If a saved profile exists → pre-populate all fields
3. User can edit fields freely
4. Checkbox **"Lưu thông tin giao hàng"** (default: checked if no saved profile, unchecked if pre-filled from saved)
5. On order submit with checkbox checked → upsert delivery profile via `PUT /api/users/me/delivery-profile`

### Location Recommendation (Future — Research Required)

Goal: Help customers fill in their delivery address faster and more accurately.

**Options to investigate:**
- **Google Places Autocomplete API** — typed address → dropdown suggestions → auto-fills structured address fields
- **Browser Geolocation API** — "Use my current location" button → reverse geocode to street address
- **Mapbox Geocoding** — alternative to Google with better pricing for high volume
- **Vietnam-specific:** Consider ViettelMap or OpenStreetMap Nominatim for local accuracy

**Recommendation for research:**  
Prototype with Google Places Autocomplete (most UX familiarity for Vietnamese users). Evaluate cost at scale before committing.

---

## 9. Pricing & Calculation Logic

All calculations performed server-side at order creation. Frontend displays values returned by the API — it does not recalculate totals independently.

```
subtotal = Σ (unitPrice × quantity) for all items
tax      = subtotal × taxRate        (taxRate configured server-side, e.g. 10%)
shipping = flatShippingFee           (or distance-based, TBD)
discount = appliedPromoDeduction     (0 if no promo)
total    = subtotal + tax + shipping - discount
```

**Why server-side only:** Prevents price manipulation by clients submitting tampered totals.

---

## 10. Cart Storage Strategy

Morii Coffee's cart uses a **hybrid storage model** — storage location depends on whether the user is authenticated.

### Decision Table

| | Guest (unauthenticated) | Logged-in |
|--|--|--|
| Storage | `localStorage` | Redis |
| Key | `morii_cart` | `cart:{userId}` |
| TTL | Browser session | 7 days (sliding) |
| Cross-device sync | ❌ | ✅ |
| On login | Merge into Redis → clear localStorage | — |

### Why Not Redis for Guests?

Redis requires a stable key to identify the user. For guests, a `sessionId` cookie could be used, but this adds session management complexity that is not justified at Morii's scale. Guests must log in before checkout anyway, so `localStorage` is a sufficient and simpler solution.

### Redis Cart Structure

```
Key:   cart:{userId}
Type:  String (JSON)
TTL:   7 days (reset on every write)

Value:
{
  "items": [
    {
      "productId": "guid",
      "variantId": "guid | null",
      "variantLabel": "Size M",
      "productName": "Cà phê sữa",
      "unitPrice": 45000,
      "quantity": 2,
      "imageUrl": "https://...",
      "addedAt": "2026-04-26T..."
    }
  ],
  "updatedAt": "2026-04-26T..."
}
```

### localStorage Cart Structure

Same JSON shape as Redis. Stored under key `morii_cart` in the browser.

### Sync Triggers

Frontend should not call the cart API on every `+`/`-` tap. Sync on these events only:

| Action | Behavior |
|---|---|
| Add item | Sync immediately |
| Remove item | Sync immediately |
| Update quantity | Debounce 500ms then sync |
| Page load | Hydrate from Redis (logged-in) or localStorage (guest) |
| Logout | Redis cart remains (user keeps cart across sessions) |

### Cart Merge on Login

When a guest logs in with items already in `localStorage`, those items must be merged into their Redis cart.

**Flow:**

```
1. User completes login
2. Frontend reads localStorage cart (morii_cart)
3. If items exist → POST /api/cart/merge { items: [...] }
4. Server: load Redis cart → merge → write back to Redis → return merged cart
5. Frontend: clear localStorage (morii_cart), update UI from API response
```

**Merge rule:** If the same `productId` + `variantId` combination exists in both carts, **add the quantities** (same convention as Shopee, Grab Food).

```
Redis cart:         [Cà phê sữa x1, Bánh mì x1]
localStorage cart:  [Cà phê sữa x2, Trà đào x1]
                         ↓ merge
Result:             [Cà phê sữa x3, Bánh mì x1, Trà đào x1]
```

### Cart API Endpoints

```
GET    /api/cart              → Load cart (Redis, logged-in only)
POST   /api/cart/items        → Add item
PUT    /api/cart/items/:id    → Update quantity
DELETE /api/cart/items/:id    → Remove item
DELETE /api/cart              → Clear cart
POST   /api/cart/merge        → Merge guest cart on login
```

---

## 11. API Contracts

### Create Order

```
POST /api/orders
Authorization: Bearer <token>

Request Body:
{
  "items": [
    {
      "productId": "guid",
      "variantId": "guid | null",
      "quantity": 2
    }
  ],
  "deliveryInfo": {
    "fullName": "Nguyễn Văn A",
    "phoneNumber": "0901234567",
    "address": "123 Đường ABC, Quận 1, TP.HCM"
  },
  "notes": "string | null",
  "paymentMethod": "COD | MOMO | PAYPAL",
  "saveDeliveryInfo": true
}

Response: 201 Created
{
  "orderId": "MRC-20250310-003"
}
```

### Get Order History

```
GET /api/orders?page=1&pageSize=10
Authorization: Bearer <token>

Response: 200 OK
{
  "items": [ OrderSummaryDto ],
  "totalCount": 42,
  "page": 1,
  "pageSize": 10
}
```

### Get Order Detail

```
GET /api/orders/:orderId
Authorization: Bearer <token>

Response: 200 OK — OrderDetailDto
```

### Get / Save Delivery Profile

```
GET  /api/users/me/delivery-profile
PUT  /api/users/me/delivery-profile
```

---

## 12. Admin Responsibilities

Admin panel order management (separate feature scope, listed here for context):

- View all orders with filters: status, date range, customer name
- Update order status (manual transitions)
- View order details
- Cancel orders (when customer requests)

Staff workflow per order:
1. New order notification → review `PENDING` order
2. Confirm → `CONFIRMED`
3. Prepare → `READY_TO_PICKUP`
4. Depart for delivery → `IN_DELIVERY`
5. Delivered → `DELIVERED`

---

## 13. Auto-Complete Mechanism

**Problem:** Customers rarely tap "I received my order" in apps, so `IN_DELIVERY` orders would never auto-progress.

**Solution:** Background job (Hangfire or .NET Hosted Service) runs daily:

```
IF order.status == IN_DELIVERY
AND order.updatedAt < (NOW - AutoCompleteAfterDays)
THEN order.status = DELIVERED
```

- Default threshold: **3 days** (configurable via `appsettings.json`)
- Log auto-completed orders for audit trail

**Alternative (deferred):** Customer-facing "Tôi đã nhận được hàng" button on Order Detail page — lower priority, not required in Phase 1.

---

## 14. UI/UX Recommendations

### From Requirements Analysis

1. **Order ID Visibility:** Display `MRC-YYYYMMDD-NNN` prominently on the success page / order detail. Customers use it for support.

2. **Optimistic Cart Updates:** Update cart item counts/totals immediately on quantity change — don't wait for an API response.

3. **Sticky Price Summary:** On long checkout forms (mobile), keep the total price visible via a sticky bottom bar showing just the total + "Đặt Giao" button.

4. **Status Progress Bar:** On Order Detail, show a horizontal step indicator (like Shopee/Grab) — more intuitive than a text list.

5. **Empty Cart Guard:** If user navigates directly to `/checkout` with an empty cart, redirect to `/cart` with a toast.

6. **Delivery Address Validation:** Phone number should validate Vietnamese formats: `09x`, `08x`, `03x`, `07x`, `05x` prefixes (10 digits total).

7. **Unsaved Changes Warning:** If user edited checkout form fields but navigates away, show a "Bạn có chắc muốn rời khỏi trang này?" confirmation.

8. **Product Availability Check on Checkout:** Re-validate product availability server-side on order creation. If a product became unavailable between cart-add and submit, return a clear error indicating which item is no longer available.

9. **Currency Format:** Display all prices in Vietnamese Dong format: `125.000đ` or `125,000 ₫` — consistent throughout.

10. **Success Feedback:** After order placement, show a brief success animation/toast before redirecting to order history. Don't immediately redirect — give the user 2–3 seconds to register that the order was placed.

---

## 15. Future Enhancements

| Enhancement | Priority | Notes |
|---|---|---|
| MoMo Payment Integration | High | Deep-link to MoMo app or redirect to MoMo payment page |
| PayPal Payment Integration | Medium | Redirect to PayPal checkout |
| Order Review / Rating | Medium | Triggered after `DELIVERED`, unlocks `REVIEWED` status |
| Promo Code / Coupon System | Medium | Apply discount codes at cart or checkout |
| Location Autocomplete | Medium | Google Places or Mapbox — see Section 8 |
| Push Notifications | Medium | Notify customer on each status change |
| Re-order Feature | Low | "Đặt lại" button on Order Detail to re-add all items to cart |
| Order Cancellation by Customer | Low | Only when status is `PENDING` |
| "Confirm Received" Button | Low | Optional customer-triggered `DELIVERED` transition |
| Estimated Delivery Time | Low | Show estimated time on order detail during `IN_DELIVERY` |

---

## 16. Out of Scope (Current Phase)

- MoMo and PayPal payment processing (UI is built, integration deferred)
- Dine-in or take-away ordering modes
- 3rd-party logistics / delivery API integration
- Customer-triggered order status updates (beyond placing the order)
- Order review / rating system
- Promotional code engine
- Admin panel order management UI (separate feature)
- Real-time order status updates via WebSocket/SSE
- Location autocomplete / map integration
