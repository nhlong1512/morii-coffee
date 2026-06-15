# VNPAY Callback Contracts

## Authoritative IPN

```http
GET /api/v1/payments/vnpay/ipn?vnp_Amount=...&vnp_TxnRef=...&vnp_SecureHash=...
```

Authentication is the verified VNPAY signature. No Morii bearer token is required.

### Response Mapping

All recognized outcomes return HTTP `200`.

| Situation | Body |
|---|---|
| Processed successfully | `{"RspCode":"00","Message":"Confirm Success"}` |
| Reference not found | `{"RspCode":"01","Message":"Order not Found"}` |
| Already confirmed | `{"RspCode":"02","Message":"Order already confirmed"}` |
| Amount mismatch | `{"RspCode":"04","Message":"Invalid Amount"}` |
| Invalid checksum | `{"RspCode":"97","Message":"Invalid Checksum"}` |
| Unexpected processing error | `{"RspCode":"99","Message":"Unknown error"}` |

### Processing Invariants

- Verify signature before business decisions.
- Verify merchant, reference, amount, response code, and transaction status.
- Success requires both response code and transaction status to equal `00`.
- Duplicate/concurrent delivery creates no duplicate order or success transition.
- A paid transaction is not regressed by a later failure callback.

## Browser Return

```http
GET /api/v1/payments/vnpay/return?vnp_Amount=...&vnp_TxnRef=...&vnp_SecureHash=...
```

The endpoint verifies authenticity but does not update order/payment state.

### Redirect

```http
302 Location: {storefront}/checkout/vnpay/return?txnRef={txnRef}&result={success|pending|failed|invalid}
```

Only sanitized values are forwarded. Secure hash and raw provider parameters are never forwarded.
