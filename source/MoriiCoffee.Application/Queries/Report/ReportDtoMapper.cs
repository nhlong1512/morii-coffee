using MoriiCoffee.Application.SeedWork.DTOs.Report;
using MoriiCoffee.Domain.Repositories;

namespace MoriiCoffee.Application.Queries.Report;

internal static class ReportDtoMapper
{
    public static AdminReportsDashboardDto ToDashboardDto(AdminReportsReadModel readModel)
    {
        return new AdminReportsDashboardDto
        {
            Range = new ReportRangeDto
            {
                From = readModel.Range.From,
                To = readModel.Range.To,
                Preset = readModel.Range.Preset,
                Granularity = readModel.Range.Granularity.ToString().ToLowerInvariant(),
                Timezone = readModel.Range.Timezone,
                ComparisonFrom = readModel.Range.ComparisonFrom,
                ComparisonTo = readModel.Range.ComparisonTo
            },
            Cards = new AdminReportsCardsDto
            {
                TotalRevenue = ToMetricCardDto(readModel.Cards.TotalRevenue),
                TotalOrders = ToMetricCardDto(readModel.Cards.TotalOrders),
                NewUsers = ToMetricCardDto(readModel.Cards.NewUsers),
                ActiveProducts = ToMetricCardDto(readModel.Cards.ActiveProducts)
            },
            RevenueSeries = new RevenueSeriesDto
            {
                Summary = new RevenueSeriesSummaryDto
                {
                    GrossRevenue = readModel.RevenueSeries.Summary.GrossRevenue,
                    RefundAmount = readModel.RevenueSeries.Summary.RefundAmount,
                    NetRevenue = readModel.RevenueSeries.Summary.NetRevenue,
                    PaidOrders = readModel.RevenueSeries.Summary.PaidOrders,
                    AverageOrderValue = readModel.RevenueSeries.Summary.AverageOrderValue,
                    Currency = readModel.RevenueSeries.Summary.Currency
                },
                Points = readModel.RevenueSeries.Points.Select(x => new RevenuePointDto
                {
                    BucketStart = x.BucketStart,
                    BucketEnd = x.BucketEnd,
                    Label = x.Label,
                    GrossRevenue = x.GrossRevenue,
                    RefundAmount = x.RefundAmount,
                    NetRevenue = x.NetRevenue,
                    PaidOrders = x.PaidOrders
                }).ToList()
            },
            OrdersByStatus = new OrderStatusBreakdownDto
            {
                TotalOrders = readModel.OrdersByStatus.TotalOrders,
                Items = readModel.OrdersByStatus.Items.Select(x => new OrderStatusBreakdownItemDto
                {
                    Status = x.Status.ToString(),
                    Count = x.Count,
                    Percentage = x.Percentage
                }).ToList()
            },
            TopProducts = new AdminReportsTopProductsDto
            {
                Items = readModel.TopProducts.Items.Select(x => new TopProductDto
                {
                    ProductId = x.ProductId,
                    ProductName = x.ProductName,
                    ThumbnailUrl = x.ThumbnailUrl,
                    UnitsSold = x.UnitsSold,
                    OrderCount = x.OrderCount,
                    GrossRevenue = x.GrossRevenue
                }).ToList()
            },
            NewUsersSeries = new NewUsersSeriesDto
            {
                TotalNewUsers = readModel.NewUsersSeries.TotalNewUsers,
                Points = readModel.NewUsersSeries.Points.Select(x => new NewUserPointDto
                {
                    BucketStart = x.BucketStart,
                    BucketEnd = x.BucketEnd,
                    Label = x.Label,
                    Users = x.Users
                }).ToList()
            }
        };
    }

    private static ReportMetricCardDto ToMetricCardDto(MetricCardReadModel readModel)
    {
        return new ReportMetricCardDto
        {
            Value = readModel.Value,
            PreviousValue = readModel.PreviousValue,
            ChangePercent = readModel.ChangePercent,
            ChangeDirection = readModel.ChangeDirection,
            ComparisonSupported = readModel.ComparisonSupported
        };
    }
}
