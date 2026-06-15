---
title: VNPAY Sandbox Setup Guide
description: Step-by-step guide to configure and test VNPAY integration in sandbox environment
nav_title: Sandbox Setup
---

# VNPAY Sandbox Setup Guide

**Duration**: ~30-45 minutes  
**Prerequisites**: Docker running locally, Morii Coffee backend cloned, ngrok or similar HTTPS tunnel tool

---

## Step 1: Obtain VNPAY Sandbox Credentials

### Register VNPAY Sandbox Account

1. Go to: https://sandbox.vnpayment.vn/
2. Create a sandbox merchant account or use existing credentials
3. Log in to the VNPAY sandbox merchant portal
4. Navigate to **Settings > Terminal Information**
5. Record these values:
   ```
   Terminal Code (Mã Cửa hàng): TMN????
   Hash Secret (Chuỗi bí mật): XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
   ```

### Enable Required APIs

In VNPAY merchant portal:

1. Go to **Settings > API Configuration**
2. Enable:
   - ✅ Payment API (Pay)
   - ✅ IPN (Instant Payment Notification)
   - ✅ Return URL
   - ✅ Query DR (QueryDR) - for reconciliation
   - ✅ Refund API (if testing refunds)

---

## Step 2: Create Public HTTPS Tunnel

VNPAY requires a public HTTPS URL to deliver IPN callbacks. Use ngrok (free option):

### Install ngrok

```bash
# macOS
brew install ngrok

# Or download from: https://ngrok.com/download
```

### Start ngrok tunnel

```bash
# Start tunnel on port 5000 (where Morii backend runs)
ngrok http 5000

# You'll see output like:
# Forwarding    https://abc123.ngrok.io -> http://localhost:5000
```

**Keep this terminal open** — ngrok will provide the public URL you need.

Record the **HTTPS URL**:
```
https://abc123.ngrok.io
```

---

## Step 3: Configure Backend Environment

### Create/Update Environment Variables

Create a `.env.sandbox` file in the `deploy/` directory:

```bash
# VNPAY Sandbox Configuration
Vnpay__TmnCode=TMN????                                    # From Step 1
Vnpay__HashSecret=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX      # From Step 1
Vnpay__PaymentUrl=https://sandbox.vnpayment.vn/paymentv2/vpcpay.html
Vnpay__ApiUrl=https://sandbox.vnpayment.vn/merchant_webapi/api/transaction
Vnpay__ReturnUrl=https://abc123.ngrok.io/api/v1/payments/vnpay/return
Vnpay__StorefrontReturnUrl=http://localhost:3000/checkout/vnpay/return
Vnpay__Locale=vn
Vnpay__PaymentExpiryMinutes=15

# Database (if needed)
ConnectionStrings__PostgresConnection=...your-db-connection...

# Stripe (keep existing)
Stripe__SecretKey=sk_test_...
Stripe__PublishableKey=pk_test_...
Stripe__WebhookSigningSecret=...
```

### Configure IPN URL in VNPAY Portal

1. Log in to VNPAY merchant portal
2. Go to **Settings > IPN Configuration**
3. Set **IPN URL** to:
   ```
   https://abc123.ngrok.io/api/v1/payments/vnpay/ipn
   ```
4. Enable IPN delivery
5. Save configuration

---

## Step 4: Start Backend with Sandbox Config

### Option A: Using Docker (Recommended)

```bash
cd deploy

# Copy env file into Docker container
export VNPAY_TMN=TMN????
export VNPAY_SECRET=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
export RETURN_URL=https://abc123.ngrok.io

bash run-docker-development.sh
```

Then in another terminal, verify the backend is running:

```bash
curl -s http://localhost:5000/api/v1/health | jq
```

### Option B: Local .NET CLI

```bash
cd source/MoriiCoffee.Presentation

# Set environment variables
export Vnpay__TmnCode=TMN????
export Vnpay__HashSecret=XXXXXXXXXXXXXXXXXXXXXXXXXXXXXXXX
export Vnpay__ReturnUrl=https://abc123.ngrok.io/api/v1/payments/vnpay/return

# Run the app
dotnet run --configuration Debug
```

**Backend is ready** when you see:
```
info: Microsoft.Hosting.Lifetime
      Now listening on: http://localhost:5000
```

---

## Step 5: Verify Backend Configuration

### Check VNPAY Startup Diagnostics

```bash
# Log in to the running backend and check logs
curl -s http://localhost:5000/api/v1/health | jq

# Should see VNPAY configuration loaded successfully
# Look for startup diagnostic logs like:
# "VnpayStartupDiagnosticsService: VNPAY configuration validated"
```

### Test Health Endpoint

```bash
curl http://localhost:5000/api/v1/health

# Expected response:
# {
#   "status": "healthy",
#   "services": {
#     "vnpay": "configured",
#     "stripe": "configured",
#     ...
#   }
# }
```

---

## Step 6: Create Test Cart & Shipping Quote

### 1. Authenticate as Test User

```bash
# Register or log in
curl -X POST http://localhost:5000/api/v1/auth/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "test@example.com",
    "password": "TestPassword123!"
  }' | jq

# Save the access_token from response
export TOKEN="eyJhbGciOiJIUzI1NiIs..."
```

### 2. Create a Cart

```bash
# Add items to cart
curl -X POST http://localhost:5000/api/v1/carts/items \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "productId": "00000000-0000-0000-0000-000000000001",
    "quantity": 1
  }' | jq
```

### 3. Get GHN Shipping Quote

```bash
# Query shipping for delivery location
curl -X POST http://localhost:5000/api/v1/shipping/quote \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "toDistrictId": 1444,  # District ID (example: Hoan Kiem, Hanoi)
    "toWardCode": "01000",
    "weight": 1000
  }' | jq

# Save the quoteId from response
export QUOTE_ID="xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx"
```

---

## Step 7: Create VNPAY Checkout

### Request Payment URL

```bash
curl -X POST http://localhost:5000/api/v1/payments/vnpay/payment-url \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "deliveryAddress": {
      "toDistrictId": 1444,
      "toWardCode": "01000",
      "streetAddress": "123 Test Street"
    },
    "shippingQuoteId": "'$QUOTE_ID'",
    "paymentMethod": "VNPAY",
    "notes": "Test order"
  }' | jq

# Response should include:
# {
#   "data": {
#     "paymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...",
#     "checkoutDraftId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
#     "txnRef": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx",
#     "amount": 125000,
#     "currency": "VND",
#     "expiresAtUtc": "2026-06-15T10:30:00Z"
#   }
# }
```

**Save these values**:
- `paymentUrl` — for browser redirect
- `checkoutDraftId` — for reconciliation
- `txnRef` — transaction reference

---

## Step 8: Complete VNPAY Test Payment

### Browser Flow (Manual Testing)

1. Copy the `paymentUrl` from Step 7
2. Open in browser: `https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?...`
3. VNPAY sandbox page loads with your payment amount
4. Select a test payment method (VNPAY provides test card numbers)
5. Complete the payment flow
6. You'll be redirected to:
   ```
   https://abc123.ngrok.io/api/v1/payments/vnpay/return?vnp_Amount=...&result=success
   ```

### Expected Backend Behavior

When payment is successful:

1. **IPN Processing** (automatic, from VNPAY → your backend)
   - Backend receives signed IPN
   - Verifies HMAC checksum ✓
   - Verifies amount ✓
   - Creates order + payment record ✓
   - Returns `{"RspCode":"00","Message":"Confirm Success"}` to VNPAY

2. **Return Redirect** (browser → storefront)
   - Backend verifies return signature
   - Redirects to frontend with sanitized parameters:
   ```
   https://localhost:3000/checkout/vnpay/return?status=success&txnRef=...
   ```

3. **Reconciliation** (optional, for testing)
   ```bash
   curl -X POST http://localhost:5000/api/v1/payments/vnpay/reconcile \
     -H "Authorization: Bearer $TOKEN" \
     -H "Content-Type: application/json" \
     -d '{
       "checkoutDraftId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
       "txnRef": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
     }' | jq
   ```

---

## Step 9: Test Key Scenarios

### Scenario 1: Successful Payment ✅

```bash
# 1. Create payment URL (Step 7)
# 2. Complete payment in VNPAY sandbox
# 3. Verify order created in database
# 4. Check payment status: PAID

# Query order
curl http://localhost:5000/api/v1/orders/{orderId} \
  -H "Authorization: Bearer $TOKEN" | jq '.data.payment'

# Expected:
# {
#   "paymentStatus": "Paid",
#   "paymentMethod": "VNPAY",
#   "provider": "Vnpay",
#   "providerSessionId": "txnRef",
#   "providerPaymentId": "vnp_TransactionNo"
# }
```

### Scenario 2: Duplicate IPN (Idempotency) ✅

```bash
# Manually replay the IPN (simulate VNPAY retry):
# 1. Save IPN query string from first payment
# 2. Call the IPN endpoint again with same parameters
# 3. Backend should return RspCode=02 (already confirmed)

curl "http://localhost:5000/api/v1/payments/vnpay/ipn?vnp_Amount=...&vnp_TxnRef=...&vnp_SecureHash=..." 

# Expected: {"RspCode":"02","Message":"Order already confirmed"}
# Result: No duplicate order created ✓
```

### Scenario 3: Invalid Amount ✅

```bash
# Modify IPN amount parameter and call
curl "http://localhost:5000/api/v1/payments/vnpay/ipn?vnp_Amount=99999&vnp_TxnRef=...&vnp_SecureHash=..."

# Expected: {"RspCode":"04","Message":"Invalid Amount"}
# Result: No payment finalized ✓
```

### Scenario 4: Invalid Checksum ✅

```bash
# Change vnp_SecureHash value
curl "http://localhost:5000/api/v1/payments/vnpay/ipn?vnp_Amount=...&vnp_TxnRef=...&vnp_SecureHash=INVALID"

# Expected: {"RspCode":"97","Message":"Invalid Checksum"}
# Result: No payment finalized ✓
```

### Scenario 5: QueryDR Reconciliation ✅

```bash
# Simulate delayed IPN - create checkout but don't trigger IPN
# Then reconcile manually:

curl -X POST http://localhost:5000/api/v1/payments/vnpay/reconcile \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "checkoutDraftId": "xxxxxxxx-xxxx-xxxx-xxxx-xxxxxxxxxxxx",
    "txnRef": "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }' | jq

# Expected: Order finalized if VNPAY reports success
# Result: Payment status updated from pending to PAID ✓
```

### Scenario 6: Return Page (No Payment Mutation) ✅

```bash
# Directly call return endpoint without IPN
curl "http://localhost:5000/api/v1/payments/vnpay/return?vnp_Amount=...&result=success&vnp_SecureHash=..."

# Expected: 302 redirect to storefront
# Result: No order created, payment status unchanged ✓
```

---

## Step 10: Enable Frontend & Test End-to-End

### Start Frontend

```bash
cd ../morii-coffee-fe

# Create .env.local for sandbox
cat > .env.local <<EOF
NEXT_PUBLIC_API_BASE_URL=https://abc123.ngrok.io/api
EOF

npm install
npm run dev

# Frontend runs on http://localhost:3000
```

### E2E Test Flow

1. **Checkout**: Select VNPAY as payment method
2. **Redirect**: Click "Complete Payment" → redirected to VNPAY sandbox
3. **Payment**: Complete VNPAY test payment
4. **Return**: Backend redirects to `http://localhost:3000/checkout/vnpay/return`
5. **Polling**: Return page polls reconcile endpoint
6. **Confirmation**: Order appears in customer's order history
7. **Admin**: Payment details visible in admin order page with VNPAY provider info

---

## Step 11: Test Refunds (Optional)

**Note**: Refund API may be restricted in sandbox. Confirm with VNPAY support.

```bash
# Request full refund
curl -X POST http://localhost:5000/api/v1/payments/{orderId}/refund \
  -H "Authorization: Bearer $ADMIN_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "amount": 125000,
    "reason": "Customer requested"
  }' | jq

# Expected response:
# {
#   "data": {
#     "refundId": "...",
#     "status": "Pending",
#     "provider": "Vnpay",
#     "providerRefundId": "..."
#   }
# }

# Reconcile refund status
curl -X POST http://localhost:5000/api/v1/payments/{orderId}/refund/reconcile \
  -H "Authorization: Bearer $ADMIN_TOKEN" | jq

# Expected: Refund status transitions to "Succeeded" or "Failed"
```

---

## Troubleshooting

### Issue: IPN Not Received

**Symptoms**: Payment completed but no order created, `paymentStatus` still `NotRequired`

**Solutions**:
1. Verify ngrok URL is correct in VNPAY merchant portal
2. Check backend logs for IPN processing errors:
   ```bash
   docker logs morii-coffee-backend | grep -i vnpay
   ```
3. Verify VNPAY__HashSecret matches in both backend config and merchant portal
4. Check ngrok logs for incoming requests:
   ```bash
   # In ngrok terminal, look for POST requests to /api/v1/payments/vnpay/ipn
   ```

### Issue: Invalid Checksum

**Symptoms**: IPN returns `RspCode=97` (Invalid Checksum)

**Solutions**:
1. Verify hash secret exactly matches VNPAY portal (no leading/trailing spaces)
2. Check that `VnpaySignatureService` canonicalization is correct
3. Run HMAC-SHA512 golden-vector test:
   ```bash
   dotnet test source/MoriiCoffee.Application.Tests \
     -k VnpaySignatureServiceTests
   ```

### Issue: Amount Mismatch

**Symptoms**: IPN returns `RspCode=04` (Invalid Amount)

**Solutions**:
1. Verify amount calculation: `(cart + shipping) * 100`
2. Check decimal precision (should be long, not int)
3. Confirm VNPAY multiply/divide by 100 is applied only once

### Issue: ngrok URL Keeps Changing

**Solutions**:
1. Use ngrok paid plan for static URL
2. Or restart backend each time ngrok URL changes
3. Record new URL and update in VNPAY merchant portal + `.env` file

---

## Checklist for Sandbox Acceptance

- [ ] VNPAY sandbox account created
- [ ] Merchant terminal code & hash secret recorded
- [ ] ngrok tunnel running with HTTPS URL
- [ ] IPN URL configured in VNPAY portal
- [ ] Backend environment variables set
- [ ] Backend starts without errors
- [ ] Test cart created with valid shipping quote
- [ ] Payment URL generated successfully
- [ ] VNPAY sandbox payment completed
- [ ] IPN received and order finalized automatically
- [ ] Return page shows correct status
- [ ] Reconciliation (QueryDR) recovers delayed IPN
- [ ] Duplicate IPN returns RspCode=02 (idempotent)
- [ ] Invalid amount/checksum rejected safely
- [ ] Stripe regression tests pass
- [ ] COD regression tests pass

---

## Next Steps After Sandbox

1. **Document Evidence**: Screenshot/log all successful scenarios
2. **Frontend Implementation**: Implement VNPAY in `morii-coffee-fe`
3. **Production Setup**: Contact VNPAY for production credentials
4. **Production Deployment**: Configure production secrets and URLs
5. **Go-Live**: Enable VNPAY for customers with monitoring

---

## Quick Reference

| Task | Command |
|------|---------|
| Start ngrok | `ngrok http 5000` |
| Run backend | `cd deploy && bash run-docker-development.sh` |
| Create payment URL | `curl -X POST http://localhost:5000/api/v1/payments/vnpay/payment-url ...` |
| Test IPN | `curl "http://localhost:5000/api/v1/payments/vnpay/ipn?vnp_Amount=...&vnp_TxnRef=...&vnp_SecureHash=..."` |
| Reconcile | `curl -X POST http://localhost:5000/api/v1/payments/vnpay/reconcile ...` |
| Check logs | `docker logs morii-coffee-backend \| grep -i vnpay` |
| Run tests | `rtk dotnet test MoriiCoffee.slnx` |

---

**Estimated Time**: 30-45 minutes for complete sandbox setup + testing  
**Questions?** Refer to [README.md](./README.md) for detailed implementation reference or contact VNPAY support.
