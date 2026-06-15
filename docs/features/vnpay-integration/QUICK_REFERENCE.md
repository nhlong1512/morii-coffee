---
title: VNPAY Integration - Quick Reference
description: Quick lookup guide for VNPAY integration files and responsibilities
nav_title: Quick Reference
---

# VNPAY Integration - Quick Reference Guide

## 📁 File Map by Layer

### 🏛️ Domain Layer (Domain-Driven)

| File | Purpose | Key Responsibility |
|------|---------|-------------------|
| `Domain.Shared/Enums/Order/EPaymentProvider.cs` | Provider enum | Defines `Stripe = 1, Vnpay = 2` |
| `Domain/Aggregates/PaymentAggregate/Payment.cs` | Payment aggregate | Provider-neutral state machine |
| `Domain/Repositories/IPaymentRepository.cs` | Repository contract | Provider-scoped lookups |

### 📱 Application Layer (CQRS)

| File | Purpose | Triggered By |
|------|---------|--------------|
| `Application/Commands/Payment/CreateVnpayPaymentUrl/` | Payment URL command | User selects VNPAY at checkout |
| `Application/Commands/Payment/HandleVnpayIpn/` | IPN handler | VNPAY calls `/ipn` webhook |
| `Application/Commands/Payment/ReconcileVnpayPayment/` | Reconcile command | User/admin calls `/reconcile` |
| `Application/Queries/Payment/GetPaymentByOrderId/` | Payment history | Admin views payment details |
| `Application/SeedWork/Abstractions/IPaymentGateway.cs` | Gateway contract | Implemented by Stripe & VNPAY |
| `Application/SeedWork/Abstractions/IPaymentGatewayResolver.cs` | Provider resolver | Routes to correct gateway |

### 🔧 Infrastructure Layer (Technical Details)

| File | Purpose | Key Logic |
|------|---------|-----------|
| `Infrastructure/Services/Payment/VnpayPaymentGateway.cs` | VNPAY gateway | URL creation, IPN parsing, QueryDR, refunds |
| `Infrastructure/Services/Payment/VnpaySignatureService.cs` | Signature crypto | HMAC-SHA512, canonicalization, verification |
| `Infrastructure/Services/Payment/VnpayClock.cs` | Time zone handling | UTC → Vietnam Time (GMT+7) |
| `Infrastructure/Services/Payment/Models/Vnpay*.cs` | VNPAY protocols | Request/response DTOs |
| `Infrastructure/Configurations/VnpayConfiguration.cs` | DI setup | Bind settings, register services |
| `Infrastructure/Services/Payment/PaymentGatewayResolver.cs` | Provider routing | Maps `EPaymentProvider` → `IPaymentGateway` |

### 🌐 Presentation Layer (HTTP Routes)

| File | Route | Auth | Purpose |
|------|-------|------|---------|
| `Presentation/Controllers/PaymentsController.cs` | `POST /api/v1/payments/vnpay/payment-url` | ✅ Authenticated | Create signed payment URL |
| `Presentation/Controllers/PaymentsController.cs` | `POST /api/v1/payments/vnpay/reconcile` | ✅ Authenticated | Query VNPAY & finalize pending |
| `Presentation/Controllers/VnpayCallbackController.cs` | `GET /api/v1/payments/vnpay/ipn` | ❌ Anonymous | Receive IPN from VNPAY |
| `Presentation/Controllers/VnpayCallbackController.cs` | `GET /api/v1/payments/vnpay/return` | ❌ Anonymous | Browser return redirect |

### 💾 Persistence Layer

| File | Purpose | Index |
|------|---------|-------|
| `Persistence/Configurations/PaymentConfiguration.cs` | Payment table config | `(provider, providerSessionId)` unique |
| `Persistence/Configurations/PaymentWebhookEventConfiguration.cs` | Webhook audit table | `(provider, providerEventId)` unique |
| `Persistence/Repositories/PaymentRepository.cs` | Payment queries | Provider-scoped lookups |
| `Persistence/Migrations/20260615023337_AddPaymentProviderOwnership` | Provider migration | Backfill existing rows as Stripe-owned |

### 🧪 Tests

| File | Coverage |
|------|----------|
| `Application.Tests/Infrastructure/Payment/VnpaySignatureServiceTests.cs` | Protocol golden vectors, tampering detection |
| `Application.Tests/Infrastructure/Payment/VnpayClock*.cs` | Time zone conversions |
| `Application.Tests/Commands/Payment/CreateVnpayPaymentUrlCommandHandlerTests.cs` | URL creation scenarios |
| `Application.Tests/Commands/Payment/HandleVnpayIpnCommandHandlerTests.cs` | IPN finalization, idempotency |
| `Application.Tests/Commands/Payment/ReconcileVnpayPaymentCommandHandlerTests.cs` | QueryDR reconciliation |

---

## 🔐 Security Responsibilities

| Component | Responsibility | Implementation |
|-----------|-----------------|-----------------|
| **VnpaySignatureService** | Signature creation & verification | HMAC-SHA512 + FixedTimeEquals() |
| **VnpayPaymentGateway** | Request/response signing | Sign outbound QueryDR/Refund requests |
| **VnpayCallbackController** | Webhook auth | Verify signature before any business logic |
| **CreateCheckoutSessionAsync()** | Amount integrity | Calculate from auth cart, never trust frontend |
| **HandleWebhookEventHandler** | Idempotency | Check `(provider, eventId)` unique index |
| **Configuration** | Secret management | All secrets from `VnpaySettings` (config-bound) |

---

## 🔄 Key Data Flows

### Flow 1: Payment URL Creation
```
User Checkout
  ↓
[CreateVnpayPaymentUrl Command]
  ↓
VnpayPaymentGateway.CreateCheckoutSessionAsync()
  • Build parameter dict (amount × 100, GMT+7 timestamps)
  • Sign with HMAC-SHA512
  • Construct URL
  ↓
[Store Checkout Draft]
  • Provider = Vnpay
  • ProviderSessionId = txnRef
  ↓
Return { paymentUrl, checkoutDraftId, txnRef }
  ↓
Frontend redirects to VNPAY payment page
```

### Flow 2: IPN Webhook (Authoritative)
```
VNPAY Payment Success
  ↓
[VnpayCallbackController.ReceiveIpn()]
  ↓
VnpayPaymentGateway.ConstructWebhookEvent()
  • Verify signature (HMAC-SHA512)
  • Check terminal code
  • Parse transaction status
  • Map to PaymentProviderEventKind
  ↓
[HandleWebhookEventCommand]
  ↓
Idempotency Check
  • Query: SELECT * FROM PaymentWebhookEvents WHERE provider=Vnpay AND eventId=...
  • If exists: return (already processed)
  ↓
[FinalizeSucceededAsync]
  • Create Payment aggregate (Provider=Vnpay)
  • Create Order (PaymentStatus=Paid)
  • Record webhook audit
  ↓
Return HTTP 200 { "RspCode": "00" }
  ↓
VNPAY stops retrying
```

### Flow 3: Browser Return (Read-Only)
```
Customer Finishes VNPAY Page
  ↓
[VnpayCallbackController.ReturnFromVnpay()]
  ↓
Verify Signature (for UI feedback only)
  ↓
No Database Updates
  ↓
Redirect to StorefrontReturnUrl?status=success
  ↓
Frontend Polls Reconcile Endpoint
  (Because browser return ≠ authoritative payment)
```

### Flow 4: Reconciliation (QueryDR)
```
User/Admin Calls Reconcile
  ↓
[ReconcileVnpayPaymentCommand]
  • Owner/admin authorization check
  • Load checkout draft
  ↓
VnpayPaymentGateway.GetCheckoutSessionStatusAsync()
  • Build signed QueryDR request
  • POST to VNPAY API
  • Verify response signature
  • Map transaction status
  ↓
If PaymentSucceeded
  → Create/Finalize Order (idempotent)
  ↓
Return { paymentStatus, orderId, expiresAt }
```

---

## 📊 Configuration Example

**appsettings.Development.json**:
```json
{
  "Vnpay": {
    "TmnCode": "LU42O52L",                    // Merchant terminal code
    "HashSecret": "OI53T8KFSCHOEFV5...",     // Signature secret (env var in production)
    "PaymentUrl": "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",
    "ApiUrl": "https://sandbox.vnpayment.vn/merchant_webapi/api/transaction",
    "ReturnUrl": "http://localhost:5000/api/v1/payments/vnpay/return",
    "StorefrontReturnUrl": "http://localhost:3000/checkout/vnpay/return",
    "Currency": "VND",
    "Locale": "vn",
    "Version": "2.1.0",
    "OrderType": "other",
    "PaymentExpiryMinutes": 15,
    "RefundEnabled": false
  }
}
```

---

## ⚡ Common Tasks

### Task: Verify a VNPAY Payment
```csharp
// 1. Check Payment aggregate
var payment = await paymentRepository.GetByProviderSessionIdAsync(
    EPaymentProvider.Vnpay, "txnRef123");
Assert.Equal(PaymentStatus.Succeeded, payment.Status);
Assert.Equal(EPaymentProvider.Vnpay, payment.Provider);

// 2. Check Order
var order = await orderRepository.GetByIdAsync(payment.OrderId);
Assert.Equal(PaymentStatus.Paid, order.PaymentStatus);
```

### Task: Process Refund
```csharp
// 1. Gateway resolves automatically based on payment.Provider
var gateway = _resolver.Resolve(payment.Provider);

// 2. Refund request
var result = await gateway.CreateRefundAsync(
    payment.ProviderSessionId,
    refundAmount,
    "Customer requested",
    cancellationToken);

// 3. Store refund record
var refund = RefundRecord.Create(payment.Id, result.ProviderRefundId, refundAmount);
await refundRepository.AddAsync(refund);
```

### Task: Handle Missing IPN
```csharp
// 1. Customer or admin calls reconcile
var command = new ReconcileVnpayPaymentCommand(checkoutDraftId, txnRef);
var result = await mediator.Send(command);

// 2. Handler calls QueryDR
var gateway = _resolver.Resolve(EPaymentProvider.Vnpay);
var status = await gateway.GetCheckoutSessionStatusAsync(txnRef);

// 3. If success, finalize order idempotently
if (status.Status == PaymentStatus.Succeeded)
{
    // Create order + payment (idempotent via unique payment index)
}
```

---

## 🔍 Debugging Checklist

| Problem | Check |
|---------|-------|
| **Signature verification fails** | ✅ TmnCode matches VNPAY portal |
| | ✅ HashSecret exact (no spaces) |
| | ✅ Canonicalization excludes vnp_SecureHash |
| | ✅ Ordinal sort (not culture-sensitive) |
| **IPN not received** | ✅ ngrok URL configured in VNPAY portal |
| | ✅ Backend listening on `/api/v1/payments/vnpay/ipn` |
| | ✅ `AllowAnonymous` on controller |
| **Amount mismatch** | ✅ Frontend didn't provide amount (backend calculates) |
| | ✅ VNPAY multiplies by 100, we multiply by 100 exactly once |
| | ✅ Division by 100 when parsing response |
| **Duplicate order created** | ✅ Webhook audit index `(provider, eventId)` enforced |
| | ✅ `HandleWebhookEventCommand` checks idempotency before finalizing |
| **Payment shows stale** | ✅ `GetPaymentByOrderIdQuery` filters by `provider` |
| | ✅ Frontend polls reconcile endpoint (return ≠ authoritative) |
| **Refund fails** | ✅ Check `RefundEnabled = true` in settings |
| | ✅ VNPAY merchant has refund API enabled |
| | ✅ Refund amount ≤ original payment amount |

---

## 📞 Support Contacts

| Issue Type | Contact |
|-----------|---------|
| VNPAY Integration Questions | VNPAY Support: support.vnpayment@vnpay.vn |
| VNPAY API Issues | VNPAY Hotline: 1900 55 55 77 |
| Morii Coffee Integration | Check [INTEGRATION_VERIFICATION.md](./INTEGRATION_VERIFICATION.md) |

---

## 📚 Related Documentation

- [IMPLEMENTATION_STATUS.md](./IMPLEMENTATION_STATUS.md) — Detailed progress report
- [INTEGRATION_VERIFICATION.md](./INTEGRATION_VERIFICATION.md) — Security & architecture verification
- [SANDBOX_SETUP.md](./SANDBOX_SETUP.md) — Step-by-step sandbox testing guide
- [README.md](./README.md) — Complete implementation guide
- [FRONTEND_HANDOFF.md](./FRONTEND_HANDOFF.md) — Frontend developer contract

---

**Last Updated**: June 15, 2026  
**Status**: ✅ COMPLETE & VERIFIED
