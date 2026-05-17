# Payment Architecture Review: Stripe Integration & Extensibility Analysis

**Date**: 2026-05-18  
**Status**: Code Review - Pre-implementation  
**Scope**: 011-stripe-payment branch  
**Focus**: Endpoint design & extensibility for future payment methods (MOMO, VNPAY)

---

## 📋 Part 1: Endpoint Architecture Issues

### Current Endpoints (❌ **NOT SCALABLE**)

```
POST /api/v1/payments/checkout-session
POST /api/v1/payments/webhook
POST /api/v1/payments/{orderId}/refund
GET  /api/v1/payments/by-order/{orderId}
```

**Problems**:

1. **Generic names, Stripe-centric behavior**
   - `checkout-session` implies only Stripe (other providers = different flow)
   - `webhook` is generic but hardcoded to only Stripe signature verification
   - When MOMO/VNPAY added, unclear how to route different webhook signatures

2. **No payment-method discrimination**
   - No way to specify which payment method's checkout to create
   - Frontend doesn't know: "Is this endpoint for Stripe, MOMO, or VNPAY?"
   - Future conflict: both STRIPE and MOMO need `/webhook` endpoints

3. **Single webhook endpoint creates routing ambiguity**
   ```
   Stripe:  signature = HMAC-SHA256(body, whsec_stripe)
   MOMO:    signature = query_param ?signature=...
   VNPAY:   signature = HMAC-SHA256(body, whsec_vnpay)
   
   → Can't route to correct handler without parsing body first
   ```

---

### ✅ **Recommended Endpoint Structure**

```
PAYMENT INITIATION
=================
POST /api/v1/payments/stripe/checkout-session     (create Stripe session)
POST /api/v1/payments/momo/checkout-session       (create MOMO session)
POST /api/v1/payments/vnpay/checkout-session      (create VNPAY session)

WEBHOOKS (Provider → Server)
============================
POST /api/v1/payments/stripe/webhook               (Stripe signature verify)
POST /api/v1/payments/momo/webhook                 (MOMO signature verify)
POST /api/v1/payments/vnpay/webhook                (VNPAY signature verify)

ADMIN OPERATIONS (Server → Admin)
=================================
POST /api/v1/payments/{orderId}/refund             (provider-agnostic, route internally)
GET  /api/v1/payments/by-order/{orderId}           (provider-agnostic)
```

**Why this structure**:
- ✅ Each provider has explicit entry points → no routing ambiguity
- ✅ Each provider's signature verification isolated → own header/query parsing
- ✅ Scalable: adding PAYPAL requires only `/api/v1/payments/paypal/*` routes
- ✅ Clear intent: API consumer knows "I need Stripe checkout" = `/stripe/checkout-session`
- ✅ Admin operations stay generic (order-centric, not payment-method-centric)

---

## 📋 Part 2: Code Extensibility Assessment

### ✅ **GOOD: Architecture**

#### 1. **IPaymentGateway Abstraction** (Excellent)
```csharp
public interface IPaymentGateway
{
    Task<CheckoutSessionResult> CreateCheckoutSessionAsync(...);
    WebhookEventEnvelope ConstructWebhookEvent(string rawBody, string? sig);
    Task<RefundResult> CreateRefundAsync(...);
    string PublishableKey { get; }
}
```

**Why it's good**:
- Application layer doesn't reference `Stripe.net` SDK
- DTOs are provider-agnostic (CreateCheckoutSessionRequest, RefundRequest, etc.)
- Single implementation per provider (StripePaymentGateway, future: MomoPaymentGateway)
- Follows Dependency Inversion — domain depends on interface, not concrete provider

**Current implementation**:
```csharp
public class StripePaymentGateway : IPaymentGateway { ... }
```

**How to extend**:
```csharp
// Future - add new implementations
public class MomoPaymentGateway : IPaymentGateway { ... }
public class VnpayPaymentGateway : IPaymentGateway { ... }
```

---

#### 2. **Payment & Domain Entities are Provider-Agnostic** ✅
```csharp
public class Payment
{
    public string StripeSessionId { get; set; }  // 🚨 Issue: why Stripe-specific?
    public string StripePaymentIntentId { get; set; }
    public string StripeChargeId { get; set; }
}

public class RefundRecord
{
    public string StripeRefundId { get; set; }  // 🚨 Same issue
}
```

**Problem**: Column names hardcoded to "Stripe"
- If MOMO session added, what's the column name? `MomoSessionId`? `PaymentIntentId`?
- Database schema becomes fragmented: `StripeSessionId`, `MomoSessionId`, `VnpaySessionId` — ugly

**Better design**:
```csharp
public class Payment
{
    public string PaymentProviderTransactionId { get; set; }  // Generic
    public string PaymentProviderPaymentIntentId { get; set; }
    // Or: PaymentProviderReference (single field for all provider ids?)
}
```

**Why current design is still acceptable** (for MVP):
- Spec says "single merchant account" + MVP is Stripe-only
- Can refactor schema later (backwards-compat migration: add generic columns, deprecate Stripe ones)
- But **be aware**: adding MOMO/VNPAY will require schema changes

---

#### 3. **Refund Command & Handler** ✅ **Provider-Agnostic**
```csharp
public class RefundPaymentCommand : ICommand<RefundResponseDto>
{
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string? Reason { get; set; }
    // No PaymentMethod field — handler looks up order → payment → provider
}

public class RefundPaymentCommandHandler : ICommandHandler<...>
{
    public async Task<RefundResponseDto> Handle(RefundPaymentCommand command, ...)
    {
        // 1. Load order
        // 2. Look up order.PaymentMethod (STRIPE, MOMO, VNPAY)
        // 3. Route to provider-specific refund implementation (IPaymentGateway.CreateRefundAsync)
        // ✅ This is good — no coupling to Stripe
    }
}
```

**Extensibility**: When MOMO added, RefundPaymentCommandHandler **does not change**. Just register `MomoPaymentGateway` in DI container → order.PaymentMethod = MOMO → handler calls gateway.CreateRefundAsync → different provider logic.

---

### ⚠️ **ISSUES: Controller & Webhook Design**

#### Issue 1: **Controllers are Stripe-Centric**

**File**: `PaymentsController.cs`
```csharp
[HttpPost("checkout-session")]
[SwaggerOperation(Summary = "Create a Stripe Checkout Session for an order")]
public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionDto dto)
{
    var result = await _mediator.Send(new CreateCheckoutSessionCommand { ... });
    return StatusCode(201, new ApiCreatedResponse(result));
}
```

**Problems**:
- Comment says "Stripe Checkout Session" but code is actually generic (CreateCheckoutSessionCommand has no PaymentMethod param)
- **Missing validation**: What if order.PaymentMethod = MOMO? This endpoint will still create a Stripe session (contradiction)

**Fix required** (MVP):
```csharp
[HttpPost("stripe/checkout-session")]
public async Task<IActionResult> CreateStripeCheckoutSession([FromBody] CreateCheckoutSessionDto dto)
{
    // Validate: order.PaymentMethod == STRIPE
    // Then: CreateCheckoutSessionCommand (same handler)
}

[HttpPost("momo/checkout-session")]  // Future
public async Task<IActionResult> CreateMomoCheckoutSession([FromBody] CreateCheckoutSessionDto dto)
{
    // Validate: order.PaymentMethod == MOMO
    // Then: CreateCheckoutSessionCommand (reuses same handler!)
}
```

---

#### Issue 2: **Webhook Endpoint is Stripe-Only** ⚠️

**File**: `PaymentWebhookController.cs`
```csharp
[Route("api/v1/payments/webhook")]
[AllowAnonymous]
public class PaymentWebhookController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Receive(CancellationToken cancellationToken)
    {
        var signature = Request.Headers["Stripe-Signature"].ToString();  // ← Stripe-specific
        
        try
        {
            envelope = _gateway.ConstructWebhookEvent(rawBody, signature);  // Stripe verification
        }
        catch (PaymentGatewaySignatureException ex) { ... }
    }
}
```

**Problems**:
1. **Only reads Stripe-Signature header**
   - MOMO sends signature as query param: `?signature=...&nonce_str=...`
   - VNPAY sends custom headers
   - Current code will fail for non-Stripe events

2. **Single webhook endpoint for all providers**
   - Webhook routing logic unclear
   - How does Stripe event reach StripePaymentGateway if next call adds MomoPaymentGateway?

**Better approach**:
```csharp
[Route("api/v1/payments/stripe/webhook")]
[AllowAnonymous]
public async Task<IActionResult> ReceiveStripe(CancellationToken cancellationToken)
{
    var signature = Request.Headers["Stripe-Signature"];  // Stripe only
    envelope = _stripeGateway.ConstructWebhookEvent(rawBody, signature);
    await _mediator.Send(new HandleWebhookEventCommand { ... });
}

[Route("api/v1/payments/momo/webhook")]
[AllowAnonymous]
public async Task<IActionResult> ReceiveMomo(CancellationToken cancellationToken)  // Future
{
    var signature = Request.Query["signature"];  // MOMO query param
    envelope = _momoGateway.ConstructWebhookEvent(rawBody, signature);
    await _mediator.Send(new HandleWebhookEventCommand { ... });
}
```

---

#### Issue 3: **WebhookEventEnvelope is Stripe-Centric** ⚠️

**File**: `IPaymentGateway.cs`
```csharp
public class WebhookEventEnvelope
{
    public string SessionId { get; set; }              // 🚨 Stripe-specific name
    public string? PaymentIntentId { get; set; }       // 🚨 Stripe-specific
    public string? ChargeId { get; set; }              // 🚨 Stripe-specific
    public IReadOnlyList<string> RefundIds { get; set; }  // 🚨 Stripe-specific
}
```

**Problem**:
- These properties are Stripe's domain model (Session, PaymentIntent, Charge)
- MOMO/VNPAY have different concepts (Order, Payment, Refund)
- The class name "Envelope" is neutral, but properties leak Stripe implementation

**Better design**:
```csharp
public class WebhookEventEnvelope
{
    public string EventId { get; set; }                // ✅ Generic
    public string EventType { get; set; }              // ✅ Generic
    public Guid? MetadataOrderId { get; set; }         // ✅ Generic (our internal id)
    public Guid? MetadataPaymentId { get; set; }       // ✅ Generic
    
    // Provider-specific transaction identifiers
    public string? ProviderSessionId { get; set; }     // Stripe: SessionId, MOMO: OrderId, etc.
    public string? ProviderPaymentId { get; set; }     // Stripe: PaymentIntentId, etc.
    public string? ProviderChargeId { get; set; }      // Stripe: ChargeId
    public IReadOnlyList<string> ProviderRefundIds { get; set; } // Provider refund refs
    
    // Better: provider-agnostic
    // public Dictionary<string, object> ProviderMetadata { get; set; }  // Flexible
}
```

**Why current design is acceptable** (for MVP):
- Spec explicitly says Stripe MVP
- Can refactor later
- But flag it as technical debt

---

### 🎯 **How to Add MOMO/VNPAY Without Breaking Current Code**

#### **Step 1: Add Payment Method Enum Value** ✅ Already done
```csharp
public enum EPaymentMethod
{
    COD = 1,
    MOMO = 2,      // ← Future: just needs to exist
    PAYPAL = 3,
    STRIPE = 4
}
```

#### **Step 2: Implement Payment Provider**
```csharp
public class MomoPaymentGateway : IPaymentGateway
{
    public async Task<CheckoutSessionResult> CreateCheckoutSessionAsync(
        CreateCheckoutSessionRequest request, ...)
    {
        // MOMO-specific: call MOMO API → get payment order
        // Return: sessionId = momoOrderId, url = momo checkout link
    }
    
    public WebhookEventEnvelope ConstructWebhookEvent(string rawBody, string? sig)
    {
        // MOMO-specific: parse query params, verify HMAC
        // Return: eventId, eventType, paymentIntentId, etc.
    }
}
```

#### **Step 3: Register in DI**
```csharp
// DependencyInjection.cs
services.AddScoped<IPaymentGateway, StripePaymentGateway>();  // Current
services.AddScoped<IPaymentGateway, MomoPaymentGateway>();    // Future
// ⚠️ Problem: both implementations resolve to IPaymentGateway!
```

**Issue**: ASP.NET DI doesn't support "multi-implementation" registration. When code calls `_gateway.CreateCheckoutSessionAsync()`, which implementation runs?

**Solution**: Use factory pattern or provider resolver:
```csharp
public interface IPaymentGatewayFactory
{
    IPaymentGateway GetGateway(EPaymentMethod method);
}

public class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly StripePaymentGateway _stripe;
    private readonly MomoPaymentGateway _momo;
    
    public IPaymentGateway GetGateway(EPaymentMethod method) => method switch
    {
        EPaymentMethod.STRIPE => _stripe,
        EPaymentMethod.MOMO => _momo,
        _ => throw new NotSupportedException($"Payment method {method} not supported")
    };
}
```

**Then in CreateCheckoutSessionCommandHandler**:
```csharp
var gateway = _gatewayFactory.GetGateway(order.PaymentMethod);
var sessionResult = await gateway.CreateCheckoutSessionAsync(request, ...);
```

#### **Step 4: Add Webhook Endpoints**
```csharp
[Route("api/v1/payments/momo/webhook")]
[AllowAnonymous]
public async Task<IActionResult> ReceiveMomo(...)
{
    var signature = Request.Query["signature"];
    var momoGateway = _gatewayFactory.GetGateway(EPaymentMethod.MOMO);
    var envelope = momoGateway.ConstructWebhookEvent(rawBody, signature);
    await _mediator.Send(new HandleWebhookEventCommand { Envelope = envelope }, ...);
}
```

---

## 📊 Summary: Extensibility Scorecard

| Aspect | Rating | Notes |
|--------|--------|-------|
| **IPaymentGateway abstraction** | ✅ Excellent | Provider-agnostic, single entry point |
| **Domain entities (Order, Payment, RefundRecord)** | ✅ Good | Minor issue: Stripe-specific column names, but OK for MVP |
| **CQRS Command/Handler pattern** | ✅ Excellent | RefundPaymentCommand works for any provider |
| **Controller design** | ⚠️ Needs work | Endpoints too generic, missing payment-method discrimination |
| **Webhook routing** | ⚠️ Critical issue | Single endpoint, provider-specific signature handling |
| **WebhookEventEnvelope** | ⚠️ Minor issue | Property names leak Stripe terminology |
| **DI registration** | ⚠️ Design needed | Multi-implementation strategy required |

---

## ✅ **ACTION ITEMS**

### **Before Merge**:
1. ✅ Rename endpoints to be payment-method-explicit:
   - `POST /api/v1/payments/stripe/checkout-session`
   - `POST /api/v1/payments/stripe/webhook`
   - Keep `POST /api/v1/payments/{orderId}/refund` (generic)

2. ⚠️ **Optional (can do in follow-up PR)**: Add payment-method validation in CreateCheckoutSessionCommand:
   ```csharp
   if (order.PaymentMethod != EPaymentMethod.STRIPE)
       throw new BadRequestException("This endpoint only supports Stripe payments");
   ```

### **Before Adding MOMO/VNPAY**:
1. Implement `IPaymentGatewayFactory` pattern
2. Update webhook endpoints: `/momo/webhook`, `/vnpay/webhook`
3. Consider renaming `WebhookEventEnvelope` properties for clarity
4. Database migration: add generic `PaymentProviderTransactionId` columns (keep Stripe ones for backwards-compat)

### **Later (Not blocking MVP)**:
1. Refactor column names in Payment & RefundRecord
2. Create provider-specific envelope types: `StripeWebhookEventEnvelope`, `MomoWebhookEventEnvelope`
3. Document webhook event type mappings (Stripe: `checkout.session.completed` → MOMO: `payment.completed`)

---

## 📌 Conclusion

**Current code is 70-80% ready for multi-provider expansion**. The main issue is **endpoint naming** (MUST fix) and **webhook routing** (MUST plan). IPaymentGateway abstraction is solid — most of the hard work is already done by design. Adding MOMO/VNPAY later is straightforward once DI factory + webhook routing are in place.
