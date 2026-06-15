# Reconcile VNPAY Payment

## Request

```http
POST /api/v1/payments/vnpay/reconcile
Authorization: Bearer {access-token}
Content-Type: application/json
```

```json
{
  "checkoutDraftId": "d5c26720-14e9-4ba8-bd10-cf737bb01a99",
  "txnRef": "d5c2672014e94ba8bd10cf737bb01a99"
}
```

At least one correlation value is required. Customers may reconcile only their own checkout; admins may reconcile supported attempts.

## Success Response

```http
200 OK
```

```json
{
  "statusCode": 200,
  "message": "Retrieved successfully",
  "data": {
    "checkoutDraftId": "d5c26720-14e9-4ba8-bd10-cf737bb01a99",
    "txnRef": "d5c2672014e94ba8bd10cf737bb01a99",
    "orderId": "7d18b2cd-2eb0-45a3-953a-bb890f11e746",
    "orderNumber": "MRC-20260615-0001",
    "paymentStatus": "Paid",
    "failureReason": null,
    "expiresAtUtc": "2026-06-15T10:30:00Z"
  }
}
```

## Behavior

1. Return local finalized state immediately when available.
2. For pending state, query VNPAY QueryDR.
3. Verify QueryDR response before applying transaction status.
4. Finalize successful state idempotently; preserve pending/failed state appropriately.

## Errors

| HTTP | Meaning |
|---|---|
| `400` | Missing correlation values or invalid request |
| `401` | Missing authentication |
| `403` | Customer does not own the checkout |
| `404` | Draft/payment cannot be found |
| `502/500` | Provider reconciliation unavailable or invalid; local state remains unchanged |
