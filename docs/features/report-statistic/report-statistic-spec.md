# Report Statistic Spec

**Status**: Draft proposal for backend ideation  
**Project**: MoriiCoffee backend repository  
**Scope**: Admin reports page currently backed by dummy data on frontend  
**Last reviewed**: 2026-05-22

## 1. Muc tieu

Tai lieu nay de xuat business definitions, API contracts, va backend responsibilities cho tinh nang `admin reports` cua MoriiCoffee.

Muc tieu cua phase nay:

- thay mock data bang backend-driven metrics
- giu contracts don gian, de frontend co the integrate ngay
- dinh nghia ro source-of-truth cho tung metric
- tranh dua vao API nhung metric ma schema hien tai chua tinh dung duoc

## 2. Repo Assessment

Spec nay da duoc doi chieu voi source backend hien tai va voi `code-review-graph`.

Nhan dinh quan trong:

- Repo hien co du primitives cho `orders`, `payments`, `refunds`, `users`, `products`
- Repo chua co module `analytics`, `reports`, hay `read model` danh rieng cho admin dashboard
- `loyalty points` khong con nam trong reports scope hien tai
- Schema hien tai khong co `store`, `channel`, hay `product status history`
- Refund dang o cap `Payment` / `Order`, khong o cap `OrderItem`

He qua thiet ke:

- Phase 1 chi nen support 5 khoi du lieu:
  - overview cards
  - revenue series
  - orders by status
  - top products
  - new users series
- Khong nen dua `loyalty`, `storeId`, `channel` vao contract phase 1
- Can ghi ro metric nao la exact, metric nao la snapshot, metric nao chi nen tra `gross`

## 3. Source Of Truth

### 3.1 Total Revenue

Khuyen nghi:

- Card chinh su dung `netRevenue`
- `netRevenue = sum(successful/recognized payments) - sum(succeeded refunds)`

Nguon du lieu:

- `Payments.Amount` voi `EPaymentTransactionStatus.Succeeded`
- `Refunds.Amount` voi `ERefundStatus.Succeeded`

Khong khuyen nghi:

- Cong truc tiep `Order.Total` cho moi order trong ky

Ly do:

- Payment layer moi la source-of-truth cho so tien da thu thuc te
- Refund duoc quan ly rieng va co trang thai settled
- `Order.Total` khong phan biet duoc payment fail, abandon, hay refunded

### 3.2 Total Orders

Khuyen nghi:

- Dem so `Orders` duoc tao trong ky theo `Order.CreatedAt`

Khong can:

- Loai bo order theo current status

Ly do:

- KPI nay nen tra loi cau hoi "co bao nhieu order duoc tao trong ky"
- Neu loai bo cancel/fail thi card se mat tinh nhat quan voi order funnel

### 3.3 New Users

Khuyen nghi:

- Dem `Users.CreatedAt` trong ky

Ghi chu:

- Neu can tach "signed up" va "activated" trong tuong lai thi do la metric khac
- Phase 1 chi nen coi `CreatedAt` la source-of-truth

### 3.4 Active Products

Khuyen nghi:

- Day la metric snapshot hien tai
- Dem `Products` co `Status == EProductStatus.Active`

Canh bao rat quan trong:

- Schema hien tai khong co lich su thay doi trang thai product
- Vi vay backend khong the tinh chinh xac `previousValue` hay `changePercent vs last period`

De xuat phase 1:

- Tra `value`
- Tra `previousValue = null`
- Tra `changePercent = null`
- Frontend nen an badge comparison cho card nay

Neu muon co comparison that:

- Phai bo sung product status audit/history hoac snapshot table theo ngay

### 3.5 Revenue Series

Khuyen nghi:

- Day la chuoi thoi gian cua `netRevenue`
- Ho tro bucket `day`, `week`, `month`
- Mac dinh granularity nen auto-suy ra theo range:
  - `7D`, `30D` => `day`
  - `90D` => `week`
  - `1Y` => `month`

Moi diem du lieu nen co:

- `grossRevenue`
- `refundAmount`
- `netRevenue`
- `paidOrders`

### 3.6 Orders By Status

Khuyen nghi phase 1:

- Lay tap order duoc tao trong ky
- Group theo `current/latest OrderStatus`

Ly do:

- UI reports hien tai phu hop voi buc tranh "orders tao trong ky da ket thuc o trang thai nao"
- Repo hien tai khong co bang lich su chuyen trang thai order

Khong nen hua:

- Funnel theo tung moc thoi gian status transition
- "as-of-date status distribution"

Muoc co nhung cai do:

- Can them order status history / domain events projection

### 3.7 Top Selling Products

Khuyen nghi phase 1:

- Aggregate tu `OrderItems` cua cac order hop le trong ky
- Order hop le la:
  - COD: order tao thanh cong
  - Stripe: order co payment da thanh cong va duoc finalise

Nhat quan hon nua:

- Dung `OrderItems.LineTotal` de tinh `grossRevenue`
- Dung `sum(OrderItems.Quantity)` de tinh `unitsSold`
- Dung `count(distinct OrderId)` de tinh `orderCount`

Canh bao rat quan trong:

- Refund hien tai o cap order/payment, khong o cap order item
- Do do `netRevenue` theo tung product se khong chinh xac neu co partial refund

De xuat phase 1:

- Endpoint top products chi nen tra `grossRevenue`
- Khong nen tra `netRevenue` o cap product

Neu business van rat muon `netRevenue` theo product:

- Phai chon 1 trong 2 huong:
  - them item-level refund allocation
  - hoac chap nhan `estimatedNetRevenue` theo ty le phan bo refund tren tong order

Khuyen nghi hien tai:

- Khong tra `estimatedNetRevenue` de tranh false precision

### 3.8 New Users Series

Khuyen nghi:

- Bucket theo `Users.CreatedAt`
- Ho tro `day`, `week`, `month`

## 4. Pham Vi Phase 1

### In scope

- Admin-only reports APIs
- Date-range filtering
- Comparison period for metrics co the tinh chinh xac
- CSV export
- Response contracts de frontend reports integrate

### Out of scope

- Loyalty points
- Store-level analytics
- Channel split (`pickup`, `delivery`, `in_store`)
- Real-time streaming dashboard
- Cohort retention
- Status transition timeline
- Per-product net revenue chinh xac khi refund la order-level

## 5. Business Rules Chot Cho Frontend

### Recommended metric definitions

1. `totalRevenue`
   - Dinh nghia: `netRevenue` trong ky
   - Cong thuc: succeeded payments - succeeded refunds

2. `totalOrders`
   - Dinh nghia: tong so order duoc tao trong ky

3. `newUsers`
   - Dinh nghia: tong so user duoc tao trong ky

4. `activeProducts`
   - Dinh nghia: tong so product dang `Active` tai thoi diem query
   - Khong comparison trong phase 1

5. `ordersByStatus`
   - Dinh nghia: cac order tao trong ky, group theo current status

6. `topProducts`
   - Dinh nghia: order items ban duoc nhieu nhat trong ky
   - Revenue cap product la `grossRevenue`

7. `newUsersSeries`
   - Dinh nghia: user registrations theo bucket

### Comparison period

Cho cac metric co comparison:

- previous period phai co cung do dai voi period hien tai
- vi du:
  - current: `2026-05-01` den `2026-05-30`
  - previous: `2026-04-01` den `2026-04-30`

Cong thuc:

- `changePercent = ((value - previousValue) / previousValue) * 100`

Quy uoc:

- neu `previousValue == 0` va `value > 0` => `changePercent = null`, `changeDirection = "up_from_zero"`
- neu `previousValue == 0` va `value == 0` => `changePercent = 0`

Khuyen nghi:

- Backend nen tra them `changeDirection` de frontend khong phai tu suy luan case zero

## 6. API Design Recommendation

### 6.1 Route strategy

Khuyen nghi tao bounded context rieng:

- `GET /api/v1/admin/reports/dashboard`
- `GET /api/v1/admin/reports/export`

Ly do:

- Reports la aggregate read-model, khong phai CRUD cua `orders`, `payments`, `users`, `products`
- Tach namespace rieng giup contract de hieu va mo rong sau nay

### 6.2 Query parameters

```ts
type ReportPreset = "7D" | "30D" | "90D" | "1Y" | "CUSTOM";
type ReportGranularity = "day" | "week" | "month";

interface AdminReportQuery {
  preset?: ReportPreset;
  from?: string; // yyyy-MM-dd
  to?: string;   // yyyy-MM-dd
  granularity?: ReportGranularity;
  timezone?: string; // optional, default "Asia/Ho_Chi_Minh"
}
```

Khuyen nghi validate:

- `preset != CUSTOM` thi `from/to` la optional
- `preset == CUSTOM` thi `from/to` la required
- `from <= to`
- range khong vuot qua gioi han phase 1, vi du `max 366 days`

### 6.3 Dashboard endpoint

`GET /api/v1/admin/reports/dashboard`

Response `data`:

```ts
interface AdminReportsDashboardResponse {
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
    totalRevenue: MetricCard;
    totalOrders: MetricCard;
    newUsers: MetricCard;
    activeProducts: SnapshotMetricCard;
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
    items: OrderStatusBreakdownItem[];
  };
  topProducts: {
    items: TopProductItem[];
  };
  newUsersSeries: {
    totalNewUsers: number;
    points: NewUserPoint[];
  };
}

interface MetricCard {
  value: number;
  previousValue: number;
  changePercent: number | null;
  changeDirection: "up" | "down" | "flat" | "up_from_zero";
}

interface SnapshotMetricCard {
  value: number;
  previousValue: null;
  changePercent: null;
  changeDirection: null;
}

interface RevenuePoint {
  bucketStart: string;
  bucketEnd: string;
  label: string;
  grossRevenue: number;
  refundAmount: number;
  netRevenue: number;
  paidOrders: number;
}

interface OrderStatusBreakdownItem {
  status:
    | "PENDING"
    | "CONFIRMED"
    | "READY_TO_PICKUP"
    | "IN_DELIVERY"
    | "DELIVERED"
    | "REVIEWED"
    | "CANCELLED";
  count: number;
  percentage: number;
}

interface TopProductItem {
  productId: string;
  productName: string;
  thumbnailUrl: string | null;
  unitsSold: number;
  orderCount: number;
  grossRevenue: number;
}

interface NewUserPoint {
  bucketStart: string;
  bucketEnd: string;
  label: string;
  users: number;
}
```

### 6.4 Export endpoint

`GET /api/v1/admin/reports/export?format=csv&preset=30D`

Khuyen nghi phase 1:

- support `format=csv`
- response la file download
- server-side export de dam bao file phu hop cung business rules voi dashboard

CSV sections nen gom:

1. Overview cards
2. Revenue series
3. Orders by status
4. Top products
5. New users series

### 6.5 Optional split endpoints

Neu muon toi uu cache hoac tai tung widget rieng, co the tach them:

- `GET /api/v1/admin/reports/overview`
- `GET /api/v1/admin/reports/revenue-series`
- `GET /api/v1/admin/reports/orders-by-status`
- `GET /api/v1/admin/reports/top-products`
- `GET /api/v1/admin/reports/new-users-series`

Tuy nhien, voi UI reports hien tai, `dashboard` endpoint la lua chon hop ly nhat de giam round-trips.

## 7. Error Contract

Response van nen di theo envelope hien tai cua repo:

```json
{
  "statusCode": 200,
  "message": "Retrieved successfully",
  "data": {},
  "errors": null
}
```

Validation / domain errors can co:

- `400` invalid range, invalid preset, invalid granularity
- `401` unauthorized
- `403` non-admin access

## 8. Backend Application Design

### 8.1 Presentation layer

Khuyen nghi tao:

- `source/MoriiCoffee.Presentation/Controllers/AdminReportsController.cs`

Authorization:

- `[Authorize(Roles = nameof(ERole.ADMIN))]`

### 8.2 Application layer

Khuyen nghi tao:

- `Queries/Report/GetAdminReportsDashboard`
- `Queries/Report/ExportAdminReports`
- `SeedWork/DTOs/Report/*`

Neu muon clean hon nua:

- `Services/Reports/ReportQueryNormalizer`
- `Services/Reports/ComparisonPeriodResolver`

### 8.3 Persistence / read-model layer

Khuyen nghi khong nhoi het vao controller/handler.

Nen co 1 abstraction chuyen cho aggregate read:

```csharp
public interface IAdminReportsReadRepository
{
    Task<DashboardOverviewDto> GetOverviewAsync(ReportRange range, CancellationToken ct);
    Task<RevenueSeriesDto> GetRevenueSeriesAsync(ReportRange range, CancellationToken ct);
    Task<OrderStatusBreakdownDto> GetOrdersByStatusAsync(ReportRange range, CancellationToken ct);
    Task<TopProductsDto> GetTopProductsAsync(ReportRange range, int limit, CancellationToken ct);
    Task<NewUsersSeriesDto> GetNewUsersSeriesAsync(ReportRange range, CancellationToken ct);
}
```

Ly do:

- Tach read-model analytics khoi generic repositories CRUD
- De sau nay co the doi sang SQL projection hay materialized view ma khong vo handler contract

## 9. Query Logic De Xuat

### 9.1 Overview cards

`totalRevenue`

- current value: `netRevenue(currentRange)`
- previous value: `netRevenue(previousRange)`

`totalOrders`

- current value: count orders by `CreatedAt` in current range
- previous value: count orders by `CreatedAt` in previous range

`newUsers`

- current value: count users by `CreatedAt` in current range
- previous value: count users by `CreatedAt` in previous range

`activeProducts`

- current value: count products where `Status == Active`
- previous value: `null`

### 9.2 Revenue series

Recommended SQL intent:

1. Xac dinh payment succeeded trong range
2. Bucket theo `Payment.CreatedAt`
3. Tong refund succeeded theo refund timestamp trong cung range
4. Tinh `net = gross - refunds` theo bucket

Luu y:

- Refund nen duoc bucket theo `Refund.CreatedAt` hoac settled timestamp cua refund
- Neu sau nay can exact settlement date, co the can them explicit `SucceededAt`

### 9.3 Orders by status

Recommended SQL intent:

1. Lay `Orders` tao trong range
2. Group theo `OrderStatus`
3. Percentage = `count / totalOrdersInRange`

### 9.4 Top products

Recommended SQL intent:

1. Lay order items cua orders tao trong range
2. Group theo `ProductId`, `ProductName`
3. Join sang `Products` de lay `ThumbnailUrl` neu can
4. Sort theo `unitsSold desc`, tie-break `grossRevenue desc`

### 9.5 New users series

Recommended SQL intent:

1. Lay `Users` co `CreatedAt` trong range
2. Bucket theo granularity
3. Dem so users moi moi bucket

## 10. Performance Strategy

### Phase 1

- Query truc tiep tu OLTP database la du
- Co the cache response `dashboard` 1-5 phut theo query key

Suggested cache key:

- `admin-reports:{preset}:{from}:{to}:{granularity}:{timezone}`

### Phase 2

Neu data lon hon:

- materialized view cho revenue/order/product aggregates
- precomputed daily fact table
- scheduled projection jobs

Khong can lam ngay trong phase 1 vi overkill cho scope hien tai.

## 11. Testing Expectations

Can cover it nhat:

1. Range normalization
2. Comparison period calculation
3. Revenue calculation with:
   - paid only
   - paid + partial refund
   - paid + full refund
   - failed/expired payment not counted
4. Orders by status aggregation
5. Top products aggregation
6. Active products snapshot returns no comparison
7. Export CSV shape
8. Admin authorization

## 12. Implementation Notes Fit Với Repo Hien Tai

Nhung de xuat nay phu hop voi style repo hien tai:

- route prefix `api/v1/...`
- response envelope `ApiOkResponse`
- MediatR query/handler pattern
- admin-only controllers da la pattern quen thuoc

Spec nay chu y tranh 3 cai bay lon:

1. Khong noi revenue bang `Order.Total` khi payment/refund da ton tai
2. Khong hua `activeProducts changePercent` khi schema chua co history
3. Khong hua `topProducts netRevenue` khi refund chua gan cap item

## 13. Final Recommendation

De frontend reports hien tai co the integrate nhanh va dung nghiep vu, backend nen implement:

- `GET /api/v1/admin/reports/dashboard`
- `GET /api/v1/admin/reports/export?format=csv`

Va backend nen chot business nhu sau:

- revenue card va revenue chart dung `netRevenue`
- top products dung `grossRevenue`
- active products la snapshot only
- orders by status la orders tao trong ky group theo current status
- new users dung `Users.CreatedAt`

Neu team dong y bo metric comparison cho `activeProducts` va bo `netRevenue` o cap product, thi spec nay da du de buoc sang phase API/detail design va implementation planning.

## 14. Open Questions Can Chot Som

Nhung diem duoi day khong bi block spec, nhung nen duoc PM/FE/BE chot truoc khi code:

1. Card `activeProducts` co chap nhan bo badge `% vs last period` khong?
   - Khuyen nghi: co, vi schema hien tai khong co history de tinh dung

2. `topProducts` nen sap xep uu tien theo `unitsSold` hay `grossRevenue`?
   - Khuyen nghi: `unitsSold desc`, tie-break `grossRevenue desc`

3. Revenue series nen bucket theo moc nao?
   - Khuyen nghi: payment/refund settled timestamps, khong theo `Order.CreatedAt`

4. Export CSV co can giong 100% layout cua frontend mock hien tai hay chi can dung so lieu?
   - Khuyen nghi: uu tien dung business truoc, layout CSV co the don gian

5. Co can support `CUSTOM` range ngay trong phase 1 khong?
   - Khuyen nghi: co, neu frontend reports da co date-range UX; neu chua co thi preset `7D/30D/90D/1Y` la du
