# API Contracts: My Wishlist

**Branch**: `014-my-wishlist` | **Date**: 2026-05-22  
**Base URL**: `http://localhost:8002/api` (development)  
**Auth**: All endpoints require `Authorization: Bearer <access_token>`

---

## Endpoints Summary

| Method | Path | Description | Auth |
|--------|------|-------------|------|
| `GET` | `/v1/wishlist` | Get user's wishlist with product snapshots | Required |
| `POST` | `/v1/wishlist/items` | Add a product to wishlist | Required |
| `DELETE` | `/v1/wishlist/items/{productId}` | Remove a product from wishlist | Required |
| `DELETE` | `/v1/wishlist` | Clear entire wishlist | Required |
| `POST` | `/v1/wishlist/merge` | Merge guest items into server wishlist | Required |

---

## GET /v1/wishlist

Retrieves the authenticated user's full wishlist with live product snapshots (current price, stock status).

**Response 200 OK**

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "items": [
      {
        "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "productName": "Iced Caramel Macchiato",
        "productSlug": "iced-caramel-macchiato",
        "basePrice": 65000.00,
        "thumbnailUrl": "https://cdn.example.com/products/macchiato.jpg",
        "inStock": true,
        "addedAt": "2026-05-20T09:00:00Z"
      }
    ],
    "updatedAt": "2026-05-20T09:00:00Z"
  }
}
```

**Notes**:
- `inStock` is derived server-side: `product.Status == EProductStatus.Active`
- Returns empty `items: []` when no wishlist exists (never 404)
- Items for deleted products are excluded from the response

---

## POST /v1/wishlist/items

Adds a product to the wishlist. Idempotent — if the product is already in the wishlist, returns 200 without creating a duplicate.

**Request Body**

```json
{
  "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6"
}
```

**Response 200 OK** (new item added or already exists)

```json
{
  "success": true,
  "statusCode": 200,
  "data": "Item added to wishlist."
}
```

**Response 404 Not Found** (product does not exist)

```json
{
  "success": false,
  "statusCode": 404,
  "message": "Product not found."
}
```

---

## DELETE /v1/wishlist/items/{productId}

Removes a specific product from the wishlist.

**Path Parameters**: `productId` (Guid)

**Response 200 OK**

```json
{
  "success": true,
  "statusCode": 200,
  "data": "Item removed from wishlist."
}
```

**Response 404 Not Found** (product not in wishlist)

```json
{
  "success": false,
  "statusCode": 404,
  "message": "Wishlist item not found."
}
```

---

## DELETE /v1/wishlist

Clears the entire wishlist for the authenticated user.

**Response 200 OK**

```json
{
  "success": true,
  "statusCode": 200,
  "data": "Wishlist cleared."
}
```

---

## POST /v1/wishlist/merge

Merges a guest user's localStorage wishlist into their server wishlist on login. Duplicate products (same productId) are preserved once — the server entry is kept, guest entry is ignored if duplicate.

**Request Body**

```json
{
  "guestItems": [
    { "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6" },
    { "productId": "1a2b3c4d-5e6f-7890-abcd-ef1234567890" }
  ]
}
```

**Response 200 OK** (returns merged wishlist)

```json
{
  "success": true,
  "statusCode": 200,
  "data": {
    "items": [
      {
        "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
        "productName": "Iced Caramel Macchiato",
        "productSlug": "iced-caramel-macchiato",
        "basePrice": 65000.00,
        "thumbnailUrl": "https://cdn.example.com/products/macchiato.jpg",
        "inStock": true,
        "addedAt": "2026-05-20T09:00:00Z"
      }
    ],
    "updatedAt": "2026-05-22T10:30:00Z"
  }
}
```

**Notes**:
- Products in `guestItems` that don't exist in the catalog are silently ignored
- Merge is additive only — existing server items are never removed during merge
- If `guestItems` is empty, returns current server wishlist unchanged

---

## Response Envelope

All endpoints use the existing `ApiOkResponse` wrapper:

```json
{
  "success": true,
  "statusCode": 200,
  "data": { ... }
}
```

Error responses use the standard error envelope already implemented in the project.
