using Microsoft.EntityFrameworkCore;
using MoriiCoffee.Domain.Aggregates.OrderAggregate;
using MoriiCoffee.Domain.Aggregates.OrderAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate;
using MoriiCoffee.Domain.Aggregates.PaymentAggregate.Entities;
using MoriiCoffee.Domain.Aggregates.ProductAggregate;
using MoriiCoffee.Domain.Aggregates.UserAggregate;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.Shared.Enums.Order;
using MoriiCoffee.Domain.Shared.Enums.Product;
using MoriiCoffee.Infrastructure.Persistence.Data;

namespace MoriiCoffee.Infrastructure.Persistence.Repositories;

/// <summary>
/// EF Core implementation of the admin reports read repository.
/// </summary>
public class AdminReportsReadRepository : IAdminReportsReadRepository
{
    private const string Currency = "VND";
    private readonly ApplicationDbContext _context;

    public AdminReportsReadRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<AdminReportsReadModel> GetDashboardAsync(
        AdminReportQueryRange range,
        CancellationToken cancellationToken)
    {
        var currentRevenue = await GetRevenueSummaryAsync(
            range.CurrentStartUtc,
            range.CurrentEndExclusiveUtc,
            cancellationToken);
        var previousRevenue = await GetRevenueSummaryAsync(
            range.ComparisonStartUtc,
            range.ComparisonEndExclusiveUtc,
            cancellationToken);

        var currentOrders = await GetOrdersCountAsync(range.CurrentStartUtc, range.CurrentEndExclusiveUtc, cancellationToken);
        var previousOrders = await GetOrdersCountAsync(range.ComparisonStartUtc, range.ComparisonEndExclusiveUtc, cancellationToken);

        var currentUsers = await GetNewUsersCountAsync(range.CurrentStartUtc, range.CurrentEndExclusiveUtc, cancellationToken);
        var previousUsers = await GetNewUsersCountAsync(range.ComparisonStartUtc, range.ComparisonEndExclusiveUtc, cancellationToken);

        var activeProducts = await _context.Products
            .Where(x => !x.IsDeleted && x.Status == EProductStatus.Active)
            .CountAsync(cancellationToken);

        var revenueSeries = await GetRevenueSeriesAsync(range, cancellationToken);
        var ordersByStatus = await GetOrdersByStatusAsync(range, cancellationToken);
        var topProducts = await GetTopProductsAsync(range, cancellationToken);
        var newUsersSeries = await GetNewUsersSeriesAsync(range, cancellationToken);

        return new AdminReportsReadModel(
            new AdminReportRangeReadModel(
                range.From,
                range.To,
                range.Preset,
                range.Granularity,
                range.Timezone,
                range.ComparisonFrom,
                range.ComparisonTo),
            new AdminReportCardsReadModel(
                BuildMetricCard(currentRevenue.NetRevenue, previousRevenue.NetRevenue),
                BuildMetricCard(currentOrders, previousOrders),
                BuildMetricCard(currentUsers, previousUsers),
                new MetricCardReadModel(activeProducts, null, null, null, false)),
            revenueSeries,
            ordersByStatus,
            topProducts,
            newUsersSeries);
    }

    private static MetricCardReadModel BuildMetricCard(decimal current, decimal previous)
    {
        if (previous == 0m && current > 0m)
            return new MetricCardReadModel(current, previous, null, "up_from_zero", true);

        if (previous == 0m)
            return new MetricCardReadModel(current, previous, 0m, "flat", true);

        var changePercent = Math.Round(((current - previous) / previous) * 100m, 2, MidpointRounding.AwayFromZero);
        var direction = changePercent switch
        {
            > 0m => "up",
            < 0m => "down",
            _ => "flat"
        };

        return new MetricCardReadModel(current, previous, changePercent, direction, true);
    }

    private static MetricCardReadModel BuildMetricCard(int current, int previous) =>
        BuildMetricCard((decimal)current, previous);

    private async Task<int> GetOrdersCountAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        return await _context.Orders
            .Where(x => !x.IsDeleted && x.CreatedAt >= fromUtc && x.CreatedAt < toUtc)
            .CountAsync(cancellationToken);
    }

    private async Task<int> GetNewUsersCountAsync(DateTime fromUtc, DateTime toUtc, CancellationToken cancellationToken)
    {
        return await _context.Users
            .Where(x => !x.IsDeleted && x.CreatedAt >= fromUtc && x.CreatedAt < toUtc)
            .CountAsync(cancellationToken);
    }

    private async Task<RevenueSeriesSummaryReadModel> GetRevenueSummaryAsync(
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken cancellationToken)
    {
        var stripeGross = await _context.Payments
            .Where(x => !x.IsDeleted &&
                        x.Status == EPaymentTransactionStatus.Succeeded &&
                        x.CreatedAt >= fromUtc &&
                        x.CreatedAt < toUtc)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var stripePaidOrderCount = await _context.Payments
            .Where(x => !x.IsDeleted &&
                        x.Status == EPaymentTransactionStatus.Succeeded &&
                        x.CreatedAt >= fromUtc &&
                        x.CreatedAt < toUtc)
            .Select(x => x.OrderId)
            .Distinct()
            .CountAsync(cancellationToken);

        var codGross = await _context.Orders
            .Where(x => !x.IsDeleted &&
                        x.PaymentMethod == EPaymentMethod.COD &&
                        (x.OrderStatus == EOrderStatus.DELIVERED || x.OrderStatus == EOrderStatus.REVIEWED) &&
                        x.CreatedAt >= fromUtc &&
                        x.CreatedAt < toUtc)
            .SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;

        var codPaidOrderCount = await _context.Orders
            .Where(x => !x.IsDeleted &&
                        x.PaymentMethod == EPaymentMethod.COD &&
                        (x.OrderStatus == EOrderStatus.DELIVERED || x.OrderStatus == EOrderStatus.REVIEWED) &&
                        x.CreatedAt >= fromUtc &&
                        x.CreatedAt < toUtc)
            .CountAsync(cancellationToken);

        var refundAmount = await _context.Refunds
            .Where(x => !x.IsDeleted &&
                        x.Status == ERefundStatus.Succeeded &&
                        x.CreatedAt >= fromUtc &&
                        x.CreatedAt < toUtc)
            .SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;

        var grossRevenue = stripeGross + codGross;
        var paidOrders = stripePaidOrderCount + codPaidOrderCount;
        var netRevenue = grossRevenue - refundAmount;
        var averageOrderValue = paidOrders > 0
            ? Math.Round(grossRevenue / paidOrders, 2, MidpointRounding.AwayFromZero)
            : 0m;

        return new RevenueSeriesSummaryReadModel(
            grossRevenue,
            refundAmount,
            netRevenue,
            paidOrders,
            averageOrderValue,
            Currency);
    }

    private async Task<RevenueSeriesReadModel> GetRevenueSeriesAsync(
        AdminReportQueryRange range,
        CancellationToken cancellationToken)
    {
        var paymentRows = await _context.Payments
            .Where(x => !x.IsDeleted &&
                        x.Status == EPaymentTransactionStatus.Succeeded &&
                        x.CreatedAt >= range.CurrentStartUtc &&
                        x.CreatedAt < range.CurrentEndExclusiveUtc)
            .Select(x => new { x.OrderId, x.Amount, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var codOrders = await _context.Orders
            .Where(x => !x.IsDeleted &&
                        x.PaymentMethod == EPaymentMethod.COD &&
                        (x.OrderStatus == EOrderStatus.DELIVERED || x.OrderStatus == EOrderStatus.REVIEWED) &&
                        x.CreatedAt >= range.CurrentStartUtc &&
                        x.CreatedAt < range.CurrentEndExclusiveUtc)
            .Select(x => new { OrderId = x.Id, Amount = x.Total, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var refunds = await _context.Refunds
            .Where(x => !x.IsDeleted &&
                        x.Status == ERefundStatus.Succeeded &&
                        x.CreatedAt >= range.CurrentStartUtc &&
                        x.CreatedAt < range.CurrentEndExclusiveUtc)
            .Select(x => new { x.Amount, x.CreatedAt })
            .ToListAsync(cancellationToken);

        var summary = await GetRevenueSummaryAsync(range.CurrentStartUtc, range.CurrentEndExclusiveUtc, cancellationToken);
        var bucketMap = CreateBuckets(range);

        foreach (var payment in paymentRows)
        {
            var bucketKey = ResolveBucketKey(payment.CreatedAt, range);
            if (!bucketMap.TryGetValue(bucketKey, out var bucket))
                continue;

            bucket.GrossRevenue += payment.Amount;
            bucket.PaidOrders.Add(payment.OrderId);
        }

        foreach (var codOrder in codOrders)
        {
            var bucketKey = ResolveBucketKey(codOrder.CreatedAt, range);
            if (!bucketMap.TryGetValue(bucketKey, out var bucket))
                continue;

            bucket.GrossRevenue += codOrder.Amount;
            bucket.PaidOrders.Add(codOrder.OrderId);
        }

        foreach (var refund in refunds)
        {
            var bucketKey = ResolveBucketKey(refund.CreatedAt, range);
            if (!bucketMap.TryGetValue(bucketKey, out var bucket))
                continue;

            bucket.RefundAmount += refund.Amount;
        }

        var points = bucketMap.Values
            .OrderBy(x => x.SortKey)
            .Select(x => new RevenuePointReadModel(
                x.BucketStart,
                x.BucketEnd,
                x.Label,
                x.GrossRevenue,
                x.RefundAmount,
                x.GrossRevenue - x.RefundAmount,
                x.PaidOrders.Count))
            .ToList();

        return new RevenueSeriesReadModel(summary, points);
    }

    private async Task<OrderStatusBreakdownReadModel> GetOrdersByStatusAsync(
        AdminReportQueryRange range,
        CancellationToken cancellationToken)
    {
        var orders = await _context.Orders
            .Where(x => !x.IsDeleted &&
                        x.CreatedAt >= range.CurrentStartUtc &&
                        x.CreatedAt < range.CurrentEndExclusiveUtc)
            .Select(x => x.OrderStatus)
            .ToListAsync(cancellationToken);

        var totalOrders = orders.Count;
        var items = orders
            .GroupBy(x => x)
            .OrderByDescending(x => x.Count())
            .ThenBy(x => x.Key)
            .Select(x => new OrderStatusBreakdownItemReadModel(
                x.Key,
                x.Count(),
                totalOrders == 0
                    ? 0m
                    : Math.Round((decimal)x.Count() / totalOrders * 100m, 2, MidpointRounding.AwayFromZero)))
            .ToList();

        return new OrderStatusBreakdownReadModel(totalOrders, items);
    }

    private async Task<TopProductsReadModel> GetTopProductsAsync(
        AdminReportQueryRange range,
        CancellationToken cancellationToken)
    {
        var validOrderIds = await _context.Orders
            .Where(x => !x.IsDeleted &&
                        x.CreatedAt >= range.CurrentStartUtc &&
                        x.CreatedAt < range.CurrentEndExclusiveUtc &&
                        ((x.PaymentMethod == EPaymentMethod.COD &&
                          (x.OrderStatus == EOrderStatus.DELIVERED || x.OrderStatus == EOrderStatus.REVIEWED)) ||
                         (x.PaymentMethod != EPaymentMethod.COD &&
                          (x.PaymentStatus == EPaymentStatus.Paid ||
                           x.PaymentStatus == EPaymentStatus.PartiallyRefunded ||
                           x.PaymentStatus == EPaymentStatus.Refunded))))
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var items = await _context.OrderItems
            .Where(x => !x.IsDeleted && validOrderIds.Contains(x.OrderId))
            .GroupBy(x => new { x.ProductId, x.ProductName })
            .Select(x => new
            {
                x.Key.ProductId,
                x.Key.ProductName,
                UnitsSold = x.Sum(y => y.Quantity),
                OrderCount = x.Select(y => y.OrderId).Distinct().Count(),
                GrossRevenue = x.Sum(y => y.LineTotal)
            })
            .OrderByDescending(x => x.UnitsSold)
            .ThenByDescending(x => x.GrossRevenue)
            .Take(10)
            .ToListAsync(cancellationToken);

        var productIds = items.Select(x => x.ProductId).Distinct().ToList();
        var thumbnails = await _context.Products
            .Where(x => !x.IsDeleted && productIds.Contains(x.Id))
            .Select(x => new { x.Id, x.ThumbnailUrl })
            .ToDictionaryAsync(x => x.Id, x => x.ThumbnailUrl, cancellationToken);

        return new TopProductsReadModel(
            items.Select(x => new TopProductReadModel(
                    x.ProductId,
                    x.ProductName,
                    thumbnails.GetValueOrDefault(x.ProductId),
                    x.UnitsSold,
                    x.OrderCount,
                    x.GrossRevenue))
                .ToList());
    }

    private async Task<NewUsersSeriesReadModel> GetNewUsersSeriesAsync(
        AdminReportQueryRange range,
        CancellationToken cancellationToken)
    {
        var users = await _context.Users
            .Where(x => !x.IsDeleted &&
                        x.CreatedAt >= range.CurrentStartUtc &&
                        x.CreatedAt < range.CurrentEndExclusiveUtc)
            .Select(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        var bucketMap = CreateBuckets(range);
        foreach (var createdAt in users)
        {
            var bucketKey = ResolveBucketKey(createdAt, range);
            if (!bucketMap.TryGetValue(bucketKey, out var bucket))
                continue;

            bucket.Users += 1;
        }

        var points = bucketMap.Values
            .OrderBy(x => x.SortKey)
            .Select(x => new NewUserPointReadModel(
                x.BucketStart,
                x.BucketEnd,
                x.Label,
                x.Users))
            .ToList();

        return new NewUsersSeriesReadModel(users.Count, points);
    }

    private static Dictionary<string, ReportBucket> CreateBuckets(AdminReportQueryRange range)
    {
        return range.Granularity switch
        {
            ReportGranularity.Day => CreateDailyBuckets(range),
            ReportGranularity.Week => CreateWeeklyBuckets(range),
            ReportGranularity.Month => CreateMonthlyBuckets(range),
            _ => throw new ArgumentOutOfRangeException(nameof(range.Granularity), range.Granularity, "Unsupported report granularity.")
        };
    }

    private static Dictionary<string, ReportBucket> CreateDailyBuckets(AdminReportQueryRange range)
    {
        var buckets = new Dictionary<string, ReportBucket>();
        for (var day = range.From; day <= range.To; day = day.AddDays(1))
        {
            var key = day.ToString("yyyy-MM-dd");
            buckets[key] = new ReportBucket(key, day, day, day.ToString("MMM d"), day.DayNumber);
        }

        return buckets;
    }

    private static Dictionary<string, ReportBucket> CreateWeeklyBuckets(AdminReportQueryRange range)
    {
        var buckets = new Dictionary<string, ReportBucket>();
        var bucketStart = range.From;
        while (bucketStart <= range.To)
        {
            var bucketEnd = bucketStart.AddDays(6);
            if (bucketEnd > range.To)
                bucketEnd = range.To;

            var key = bucketStart.ToString("yyyy-MM-dd");
            buckets[key] = new ReportBucket(key, bucketStart, bucketEnd, $"{bucketStart:MMM d} - {bucketEnd:MMM d}", bucketStart.DayNumber);
            bucketStart = bucketEnd.AddDays(1);
        }

        return buckets;
    }

    private static Dictionary<string, ReportBucket> CreateMonthlyBuckets(AdminReportQueryRange range)
    {
        var buckets = new Dictionary<string, ReportBucket>();
        var cursor = new DateOnly(range.From.Year, range.From.Month, 1);
        while (cursor <= range.To)
        {
            var monthStart = cursor < range.From ? range.From : cursor;
            var lastDayOfMonth = DateTime.DaysInMonth(cursor.Year, cursor.Month);
            var monthEnd = new DateOnly(cursor.Year, cursor.Month, lastDayOfMonth);
            if (monthEnd > range.To)
                monthEnd = range.To;

            var key = cursor.ToString("yyyy-MM");
            buckets[key] = new ReportBucket(key, monthStart, monthEnd, cursor.ToString("MMM yyyy"), monthStart.DayNumber);
            cursor = cursor.AddMonths(1);
        }

        return buckets;
    }

    private static string ResolveBucketKey(DateTime utcDateTime, AdminReportQueryRange range)
    {
        var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(range.Timezone);
        var localDate = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, timeZoneInfo));
        return range.Granularity switch
        {
            ReportGranularity.Day => localDate.ToString("yyyy-MM-dd"),
            ReportGranularity.Week => ResolveWeeklyBucketKey(localDate, range.From),
            ReportGranularity.Month => localDate.ToString("yyyy-MM"),
            _ => throw new ArgumentOutOfRangeException(nameof(range.Granularity), range.Granularity, "Unsupported report granularity.")
        };
    }

    private static string ResolveWeeklyBucketKey(DateOnly localDate, DateOnly rangeStart)
    {
        var diff = localDate.DayNumber - rangeStart.DayNumber;
        var weekOffset = diff / 7;
        var bucketStart = rangeStart.AddDays(weekOffset * 7);
        return bucketStart.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Mutable internal bucket used while composing report sections.
    /// </summary>
    private sealed class ReportBucket
    {
        public ReportBucket(string key, DateOnly bucketStart, DateOnly bucketEnd, string label, int sortKey)
        {
            Key = key;
            BucketStart = bucketStart;
            BucketEnd = bucketEnd;
            Label = label;
            SortKey = sortKey;
        }

        public string Key { get; }

        public DateOnly BucketStart { get; }

        public DateOnly BucketEnd { get; }

        public string Label { get; }

        public int SortKey { get; }

        public decimal GrossRevenue { get; set; }

        public decimal RefundAmount { get; set; }

        public int Users { get; set; }

        public HashSet<Guid> PaidOrders { get; } = [];
    }
}
