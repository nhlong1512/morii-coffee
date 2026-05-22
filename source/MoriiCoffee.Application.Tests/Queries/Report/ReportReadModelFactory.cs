using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.Shared.Enums.Order;

namespace MoriiCoffee.Application.Tests.Queries.Report;

internal static class ReportReadModelFactory
{
    public static AdminReportsReadModel Create()
    {
        return new AdminReportsReadModel(
            new AdminReportRangeReadModel(
                new DateOnly(2026, 04, 24),
                new DateOnly(2026, 05, 22),
                "30D",
                ReportGranularity.Day,
                "Asia/Ho_Chi_Minh",
                new DateOnly(2026, 03, 25),
                new DateOnly(2026, 04, 23)),
            new AdminReportCardsReadModel(
                new MetricCardReadModel(1250000m, 1000000m, 25m, "up", true),
                new MetricCardReadModel(42m, 40m, 5m, "up", true),
                new MetricCardReadModel(15m, 10m, 50m, "up", true),
                new MetricCardReadModel(18m, null, null, null, false)),
            new RevenueSeriesReadModel(
                new RevenueSeriesSummaryReadModel(1500000m, 250000m, 1250000m, 20, 75000m, "VND"),
                [new RevenuePointReadModel(new DateOnly(2026, 05, 22), new DateOnly(2026, 05, 22), "May 22", 120000m, 0m, 120000m, 2)]),
            new OrderStatusBreakdownReadModel(
                10,
                [new OrderStatusBreakdownItemReadModel(EOrderStatus.DELIVERED, 7, 70m)]),
            new TopProductsReadModel(
                [new TopProductReadModel(Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), "Iced Americano", null, 12, 8, 540000m)]),
            new NewUsersSeriesReadModel(
                3,
                [new NewUserPointReadModel(new DateOnly(2026, 05, 22), new DateOnly(2026, 05, 22), "May 22", 3)]));
    }

    public static AdminReportsReadModel CreateZeroActivity()
    {
        return new AdminReportsReadModel(
            new AdminReportRangeReadModel(
                new DateOnly(2026, 04, 24),
                new DateOnly(2026, 05, 22),
                "30D",
                ReportGranularity.Day,
                "Asia/Ho_Chi_Minh",
                new DateOnly(2026, 03, 25),
                new DateOnly(2026, 04, 23)),
            new AdminReportCardsReadModel(
                new MetricCardReadModel(50000m, 0m, null, "up_from_zero", true),
                new MetricCardReadModel(0m, 0m, 0m, "flat", true),
                new MetricCardReadModel(0m, 0m, 0m, "flat", true),
                new MetricCardReadModel(18m, null, null, null, false)),
            new RevenueSeriesReadModel(
                new RevenueSeriesSummaryReadModel(0m, 0m, 0m, 0, 0m, "VND"),
                [new RevenuePointReadModel(new DateOnly(2026, 05, 22), new DateOnly(2026, 05, 22), "May 22", 0m, 0m, 0m, 0)]),
            new OrderStatusBreakdownReadModel(0, []),
            new TopProductsReadModel([]),
            new NewUsersSeriesReadModel(0, [new NewUserPointReadModel(new DateOnly(2026, 05, 22), new DateOnly(2026, 05, 22), "May 22", 0)]));
    }
}
