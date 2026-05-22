using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Domain.Repositories;

/// <summary>
/// Read-only repository contract for admin reporting aggregates composed from existing
/// operational data sources such as orders, payments, refunds, products, and users.
/// </summary>
public interface IAdminReportsReadRepository
{
    /// <summary>
    /// Returns the complete dashboard read model for the requested reporting range.
    /// </summary>
    Task<AdminReportsReadModel> GetDashboardAsync(AdminReportQueryRange range, CancellationToken cancellationToken);
}

/// <summary>
/// Supported granularity options for reports time bucketing.
/// </summary>
public enum ReportGranularity
{
    Day = 1,
    Week = 2,
    Month = 3
}

/// <summary>
/// Normalized reporting range used by the application and persistence layers.
/// </summary>
public sealed record AdminReportQueryRange(
    string? Preset,
    DateOnly From,
    DateOnly To,
    ReportGranularity Granularity,
    string Timezone,
    DateOnly ComparisonFrom,
    DateOnly ComparisonTo,
    DateTime CurrentStartUtc,
    DateTime CurrentEndExclusiveUtc,
    DateTime ComparisonStartUtc,
    DateTime ComparisonEndExclusiveUtc);

/// <summary>
/// Full read model returned from the admin reports aggregation pipeline.
/// </summary>
public sealed record AdminReportsReadModel(
    AdminReportRangeReadModel Range,
    AdminReportCardsReadModel Cards,
    RevenueSeriesReadModel RevenueSeries,
    OrderStatusBreakdownReadModel OrdersByStatus,
    TopProductsReadModel TopProducts,
    NewUsersSeriesReadModel NewUsersSeries);

/// <summary>
/// Reporting-period metadata returned with the dashboard payload.
/// </summary>
public sealed record AdminReportRangeReadModel(
    DateOnly From,
    DateOnly To,
    string? Preset,
    ReportGranularity Granularity,
    string Timezone,
    DateOnly ComparisonFrom,
    DateOnly ComparisonTo);

/// <summary>
/// Group of summary cards displayed in the reports header area.
/// </summary>
public sealed record AdminReportCardsReadModel(
    MetricCardReadModel TotalRevenue,
    MetricCardReadModel TotalOrders,
    MetricCardReadModel NewUsers,
    MetricCardReadModel ActiveProducts);

/// <summary>
/// Generic metric card value object.
/// </summary>
public sealed record MetricCardReadModel(
    decimal Value,
    decimal? PreviousValue,
    decimal? ChangePercent,
    string? ChangeDirection,
    bool ComparisonSupported);

/// <summary>
/// Revenue-series section read model.
/// </summary>
public sealed record RevenueSeriesReadModel(
    RevenueSeriesSummaryReadModel Summary,
    IReadOnlyList<RevenuePointReadModel> Points);

/// <summary>
/// Revenue-summary values for the selected reporting range.
/// </summary>
public sealed record RevenueSeriesSummaryReadModel(
    decimal GrossRevenue,
    decimal RefundAmount,
    decimal NetRevenue,
    int PaidOrders,
    decimal AverageOrderValue,
    string Currency);

/// <summary>
/// One time bucket in the revenue trend series.
/// </summary>
public sealed record RevenuePointReadModel(
    DateOnly BucketStart,
    DateOnly BucketEnd,
    string Label,
    decimal GrossRevenue,
    decimal RefundAmount,
    decimal NetRevenue,
    int PaidOrders);

/// <summary>
/// Order-status breakdown section read model.
/// </summary>
public sealed record OrderStatusBreakdownReadModel(
    int TotalOrders,
    IReadOnlyList<OrderStatusBreakdownItemReadModel> Items);

/// <summary>
/// One grouped item in the order-status breakdown.
/// </summary>
public sealed record OrderStatusBreakdownItemReadModel(
    EOrderStatus Status,
    int Count,
    decimal Percentage);

/// <summary>
/// Top-products section read model.
/// </summary>
public sealed record TopProductsReadModel(
    IReadOnlyList<TopProductReadModel> Items);

/// <summary>
/// One ranked top-selling product.
/// </summary>
public sealed record TopProductReadModel(
    Guid ProductId,
    string ProductName,
    string? ThumbnailUrl,
    int UnitsSold,
    int OrderCount,
    decimal GrossRevenue);

/// <summary>
/// New-user growth section read model.
/// </summary>
public sealed record NewUsersSeriesReadModel(
    int TotalNewUsers,
    IReadOnlyList<NewUserPointReadModel> Points);

/// <summary>
/// One time bucket in the new-user growth series.
/// </summary>
public sealed record NewUserPointReadModel(
    DateOnly BucketStart,
    DateOnly BucketEnd,
    string Label,
    int Users);
