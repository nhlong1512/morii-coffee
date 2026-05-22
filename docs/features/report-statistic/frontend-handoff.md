# Admin Reports Frontend Handoff

Tài liệu này mô tả contract backend thực tế đã implement cho tính năng `admin reports`, để team frontend có thể tiếp tục tích hợp tại `/admin/reports`.

## Scope phase 1

Phase 1 hiện hỗ trợ 2 API:

1. `GET /api/v1/admin/reports/dashboard`
2. `GET /api/v1/admin/reports/export`

Reports hiện chỉ cover 5 khối dữ liệu:

1. Summary cards
2. Revenue series
3. Orders by status
4. Top products
5. New users series

Ngoài scope phase 1:

1. Loyalty points
2. Store/channel split
3. Product-level net revenue
4. Historical comparison cho `activeProducts`

## Auth

- Cả 2 API đều yêu cầu `ADMIN`
- Nếu thiếu token: `401`
- Nếu có token nhưng không phải admin: `403`

## Response envelope

Dashboard API trả về envelope chuẩn:

```json
{
  "statusCode": 200,
  "message": "Retrieved successfully",
  "data": {}
}
```

Shape tổng quát:

```ts
type ApiResponse<T> = {
  statusCode: number;
  message: string;
  data?: T;
  errors?: string[];
};
```

Lưu ý:

- `export` API không trả envelope JSON
- `export` trả thẳng file CSV

## 1. Dashboard API

### Endpoint

```http
GET /api/v1/admin/reports/dashboard
```

### Query params

```ts
type ReportPreset = "7D" | "30D" | "90D" | "1Y" | "CUSTOM";
type ReportGranularity = "day" | "week" | "month";
```

| Param | Type | Required | Notes |
|---|---|---:|---|
| `preset` | `ReportPreset` | No | Default là `30D` nếu không truyền |
| `from` | `YYYY-MM-DD` | Conditional | Bắt buộc khi `preset=CUSTOM` |
| `to` | `YYYY-MM-DD` | Conditional | Bắt buộc khi `preset=CUSTOM` |
| `granularity` | `day \| week \| month` | No | Có thể override bucket |
| `timezone` | string | No | Default là `Asia/Ho_Chi_Minh` |

### Range normalization rules

- `preset` không truyền: backend tự dùng `30D`
- `CUSTOM` yêu cầu đủ cả `from` và `to`
- `from <= to`
- Max range: `366` ngày inclusive
- Auto granularity nếu FE không truyền:
  - `<= 31` ngày: `day`
  - `<= 120` ngày: `week`
  - `> 120` ngày: `month`
- `comparisonFrom` / `comparisonTo` luôn là previous period có cùng số ngày inclusive

Ví dụ:

- `30D` ngày `2026-05-22` theo timezone `Asia/Ho_Chi_Minh` sẽ normalize thành:
  - `from = 2026-04-23`
  - `to = 2026-05-22`
  - `comparisonFrom = 2026-03-24`
  - `comparisonTo = 2026-04-22`

### Response shape

```ts
type AdminReportsDashboardResponse = {
  range: {
    from: string;
    to: string;
    preset: ReportPreset | null;
    granularity: ReportGranularity;
    timezone: string;
    comparisonFrom: string;
    comparisonTo: string;
  };
  cards: {
    totalRevenue: ReportMetricCard;
    totalOrders: ReportMetricCard;
    newUsers: ReportMetricCard;
    activeProducts: ReportMetricCard;
  };
  revenueSeries: {
    summary: {
      grossRevenue: number;
      refundAmount: number;
      netRevenue: number;
      paidOrders: number;
      averageOrderValue: number;
      currency: "VND";
    };
    points: RevenuePoint[];
  };
  ordersByStatus: {
    totalOrders: number;
    items: OrderStatusItem[];
  };
  topProducts: {
    items: TopProduct[];
  };
  newUsersSeries: {
    totalNewUsers: number;
    points: NewUserPoint[];
  };
};

type ReportMetricCard = {
  value: number;
  previousValue: number | null;
  changePercent: number | null;
  changeDirection: "up" | "down" | "flat" | "up_from_zero" | null;
  comparisonSupported: boolean;
};

type RevenuePoint = {
  bucketStart: string;
  bucketEnd: string;
  label: string;
  grossRevenue: number;
  refundAmount: number;
  netRevenue: number;
  paidOrders: number;
};

type OrderStatusItem = {
  status: OrderStatus;
  count: number;
  percentage: number;
};

type TopProduct = {
  productId: string;
  productName: string;
  thumbnailUrl: string | null;
  unitsSold: number;
  orderCount: number;
  grossRevenue: number;
};

type NewUserPoint = {
  bucketStart: string;
  bucketEnd: string;
  label: string;
  users: number;
};
```

### Sample response

```json
{
  "statusCode": 200,
  "message": "Retrieved successfully",
  "data": {
    "range": {
      "from": "2026-04-23",
      "to": "2026-05-22",
      "preset": "30D",
      "granularity": "day",
      "timezone": "Asia/Ho_Chi_Minh",
      "comparisonFrom": "2026-03-24",
      "comparisonTo": "2026-04-22"
    },
    "cards": {
      "totalRevenue": {
        "value": 12500000,
        "previousValue": 11800000,
        "changePercent": 5.93,
        "changeDirection": "up",
        "comparisonSupported": true
      },
      "totalOrders": {
        "value": 420,
        "previousValue": 398,
        "changePercent": 5.53,
        "changeDirection": "up",
        "comparisonSupported": true
      },
      "newUsers": {
        "value": 91,
        "previousValue": 74,
        "changePercent": 22.97,
        "changeDirection": "up",
        "comparisonSupported": true
      },
      "activeProducts": {
        "value": 37,
        "previousValue": null,
        "changePercent": null,
        "changeDirection": null,
        "comparisonSupported": false
      }
    },
    "revenueSeries": {
      "summary": {
        "grossRevenue": 13200000,
        "refundAmount": 700000,
        "netRevenue": 12500000,
        "paidOrders": 401,
        "averageOrderValue": 31172.07,
        "currency": "VND"
      },
      "points": [
        {
          "bucketStart": "2026-05-01",
          "bucketEnd": "2026-05-01",
          "label": "May 1",
          "grossRevenue": 450000,
          "refundAmount": 0,
          "netRevenue": 450000,
          "paidOrders": 14
        }
      ]
    },
    "ordersByStatus": {
      "totalOrders": 420,
      "items": [
        {
          "status": "DELIVERED",
          "count": 250,
          "percentage": 59.52
        }
      ]
    },
    "topProducts": {
      "items": [
        {
          "productId": "00000000-0000-0000-0000-000000000000",
          "productName": "Iced Americano",
          "thumbnailUrl": "https://cdn.example.com/products/americano.jpg",
          "unitsSold": 120,
          "orderCount": 90,
          "grossRevenue": 4800000
        }
      ]
    },
    "newUsersSeries": {
      "totalNewUsers": 91,
      "points": [
        {
          "bucketStart": "2026-05-01",
          "bucketEnd": "2026-05-01",
          "label": "May 1",
          "users": 4
        }
      ]
    }
  }
}
```

## 2. Export API

### Endpoint

```http
GET /api/v1/admin/reports/export
```

### Query params

Giống dashboard API, cộng thêm:

| Param | Type | Required | Notes |
|---|---|---:|---|
| `format` | `"csv"` | Yes | Phase 1 chỉ support `csv` |

Ví dụ:

```http
GET /api/v1/admin/reports/export?format=csv&preset=30D&granularity=day&timezone=Asia/Ho_Chi_Minh
```

### Response

- `200 OK`
- `Content-Type: text/csv`
- trả file download
- filename format:

```txt
admin-reports-YYYYMMDD-YYYYMMDD.csv
```

Ví dụ:

```txt
admin-reports-20260423-20260522.csv
```

### CSV content

CSV hiện được backend xuất theo nhiều section:

1. `Range`
2. `SummaryCard`
3. `RevenueSummary`
4. `RevenuePoint`
5. `OrdersByStatus`
6. `OrdersByStatusItem`
7. `TopProduct`
8. `NewUsers`
9. `NewUserPoint`

FE không cần parse CSV để render page. API này chỉ dùng cho export button.

## Business logic by section

### 1. `cards.totalRevenue`

Semantic:

- `value` = `netRevenue`
- `netRevenue = grossRevenue - refundAmount`

`grossRevenue` hiện được tính từ:

1. Stripe payments có `Payment.Status = Succeeded`
2. COD orders có:
   - `PaymentMethod = COD`
   - `OrderStatus = DELIVERED | REVIEWED`

`refundAmount` hiện được tính từ:

1. Refund records có `Refund.Status = Succeeded`

FE implication:

- label card nên là `Total Revenue`, nhưng hiểu là net revenue
- badge comparison lấy trực tiếp từ backend, không tự tính lại ở client

### 2. `cards.totalOrders`

Semantic:

- số lượng order được `CreatedAt` trong kỳ
- không filter theo status

### 3. `cards.newUsers`

Semantic:

- số lượng user có `CreatedAt` trong kỳ

### 4. `cards.activeProducts`

Semantic:

- snapshot số products hiện đang `Status = Active`
- không phải historical metric trong kỳ
- không có comparison với previous period

FE implication:

- nếu UI card có badge mặc định, cần ẩn badge khi `comparisonSupported = false`

### 5. `revenueSeries`

Semantic:

- cùng business truth với `totalRevenue`
- bucket theo `day | week | month`
- timezone-aware

Important:

- `summary.netRevenue` là source tốt nhất để hiển thị tổng doanh thu
- `points[].netRevenue` nên là series chính để vẽ chart
- `summary.currency` hiện cố định là `VND`

### 6. `ordersByStatus`

Semantic:

- lấy tất cả orders `CreatedAt` trong kỳ
- group theo `OrderStatus` hiện tại
- `percentage` là tỷ lệ trên `totalOrders`

### 7. `topProducts`

Semantic:

- chỉ tính trên recognized/paid orders
- order hợp lệ hiện gồm:
  - COD + `DELIVERED | REVIEWED`
  - non-COD + `PaymentStatus = Paid | PartiallyRefunded | Refunded`
- ranking hiện tại:
  - sort `UnitsSold DESC`
  - tie-break bằng `GrossRevenue DESC`
- chỉ trả `grossRevenue`

FE implication:

- nếu UI muốn “Top selling products”, nên rank/display theo `unitsSold`
- nếu UI muốn secondary column, dùng `grossRevenue`

### 8. `newUsersSeries`

Semantic:

- bucket số lượng users mới theo `CreatedAt`
- cùng `range` và `granularity` với toàn dashboard

## Enum values FE nên biết

### Order status

```ts
type OrderStatus =
  | "PENDING"
  | "CONFIRMED"
  | "READY_TO_PICKUP"
  | "IN_DELIVERY"
  | "DELIVERED"
  | "REVIEWED"
  | "CANCELLED";
```

### Product status

Backend internal:

```ts
type ProductStatus = "Active" | "Inactive" | "OutOfStock";
```

FE reports hiện không nhận `productStatus` trực tiếp từ API này, nhưng `activeProducts` được derive từ `Active`.

### Payment-related enums

Không trả trực tiếp ở reports API, nhưng hữu ích để hiểu business:

```ts
type PaymentMethod = "COD" | "MOMO" | "PAYPAL" | "STRIPE";

type PaymentStatus =
  | "NotRequired"
  | "Pending"
  | "Paid"
  | "Failed"
  | "Refunded"
  | "PartiallyRefunded";

type PaymentTransactionStatus = "Created" | "Succeeded" | "Failed" | "Expired";

type RefundStatus = "Pending" | "Succeeded" | "Failed";
```

## FE behavior recommendations

### Comparison badge

Handle 4 cases:

1. `comparisonSupported = false`
   - ẩn badge hoặc hiển thị `N/A`
2. `changeDirection = "up"`
   - badge tăng
3. `changeDirection = "down"`
   - badge giảm
4. `changeDirection = "flat"`
   - badge trung tính
5. `changeDirection = "up_from_zero"`
   - không nên hiển thị `%`
   - nên hiển thị text dạng `New vs last period` hoặc `Increased from 0`

### Zero-data range

Backend vẫn trả đủ section khi range không có data:

- cards có thể bằng `0`
- chart points vẫn tồn tại nhưng value `0`
- arrays có thể rỗng, đặc biệt `ordersByStatus.items` và `topProducts.items`

FE không nên:

- coi `[]` là lỗi
- ẩn section chỉ vì không có data

### Currency formatting

- backend trả number raw
- FE tự format `VND`
- nên dùng locale VN, ví dụ `vi-VN`

### Granularity UX

Nếu FE cho user chọn granularity thủ công:

- có thể gửi `day`, `week`, `month` trực tiếp
- nếu không gửi, backend tự infer

Nếu FE không có custom granularity picker:

- chỉ cần gửi `preset`
- backend vẫn trả `range.granularity` thực tế để FE label chart chính xác

## Error handling

### `400 Bad Request`

Một số case phổ biến:

1. `preset` không hợp lệ
2. `granularity` không hợp lệ
3. `timezone` không hợp lệ
4. `preset=CUSTOM` nhưng thiếu `from` hoặc `to`
5. `from > to`
6. range > `366` ngày
7. export `format` khác `csv`

FE nên hiển thị `message` hoặc `errors`

### `401` / `403`

- chuyển user về luồng auth / unauthorized state như các admin page khác

## Suggested FE integration types

```ts
export type ReportPreset = "7D" | "30D" | "90D" | "1Y" | "CUSTOM";
export type ReportGranularity = "day" | "week" | "month";

export type ReportsDashboardQuery = {
  preset?: ReportPreset;
  from?: string;
  to?: string;
  granularity?: ReportGranularity;
  timezone?: string;
};

export type ReportsExportQuery = ReportsDashboardQuery & {
  format: "csv";
};
```

## Suggested FE API examples

```ts
const res = await api.get<ApiResponse<AdminReportsDashboardResponse>>(
  "/api/v1/admin/reports/dashboard",
  {
    params: {
      preset: "30D",
      granularity: "day",
      timezone: "Asia/Ho_Chi_Minh",
    },
  }
);

const data = res.data.data;
```

Export:

```ts
const res = await api.get("/api/v1/admin/reports/export", {
  params: {
    format: "csv",
    preset: "30D",
    granularity: "day",
    timezone: "Asia/Ho_Chi_Minh",
  },
  responseType: "blob",
});
```

## Recommended UI mapping

| UI section | Backend field |
|---|---|
| Total Revenue card | `data.cards.totalRevenue` |
| Total Orders card | `data.cards.totalOrders` |
| New Users card | `data.cards.newUsers` |
| Active Products card | `data.cards.activeProducts` |
| Revenue chart | `data.revenueSeries.points` |
| Revenue summary chips | `data.revenueSeries.summary` |
| Orders by Status chart | `data.ordersByStatus.items` |
| Top Products table | `data.topProducts.items` |
| New Users chart | `data.newUsersSeries.points` |

## Final notes for frontend

1. Dùng trực tiếp số comparison backend trả về, không tự tính lại.
2. `activeProducts` là snapshot-only, đừng render badge tăng giảm như các card khác.
3. `topProducts.grossRevenue` là gross, không phải net.
4. `totalRevenue` và `revenueSeries.netRevenue` mới là số doanh thu “thật” nên ưu tiên hiển thị.
5. Export hiện chỉ là CSV, không có PDF/XLSX trong phase 1.
