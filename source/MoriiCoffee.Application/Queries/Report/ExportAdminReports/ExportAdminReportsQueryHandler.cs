using System.Text;
using MoriiCoffee.Application.SeedWork.DTOs.Report;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Reports;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Report.ExportAdminReports;

/// <summary>
/// Generates a CSV export mirroring the selected admin reports dashboard view.
/// </summary>
public class ExportAdminReportsQueryHandler : IQueryHandler<ExportAdminReportsQuery, AdminReportsExportDto>
{
    private readonly ReportQueryNormalizer _reportQueryNormalizer;
    private readonly IUnitOfWork _unitOfWork;

    public ExportAdminReportsQueryHandler(
        ReportQueryNormalizer reportQueryNormalizer,
        IUnitOfWork unitOfWork)
    {
        _reportQueryNormalizer = reportQueryNormalizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminReportsExportDto> Handle(
        ExportAdminReportsQuery request,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Format) &&
            !string.Equals(request.Format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            throw new BadRequestException("Only csv export format is supported for admin reports.");
        }

        var range = _reportQueryNormalizer.Normalize(
            request.Preset,
            request.From,
            request.To,
            request.Granularity,
            request.Timezone);

        var readModel = await _unitOfWork.AdminReports.GetDashboardAsync(range, cancellationToken);
        var dashboard = ReportDtoMapper.ToDashboardDto(readModel);
        var content = BuildCsv(dashboard);

        return new AdminReportsExportDto
        {
            Content = Encoding.UTF8.GetBytes(content),
            ContentType = "text/csv",
            FileName = $"admin-reports-{dashboard.Range.From:yyyyMMdd}-{dashboard.Range.To:yyyyMMdd}.csv"
        };
    }

    private static string BuildCsv(AdminReportsDashboardDto dashboard)
    {
        var lines = new List<string>
        {
            "Section,Key,Value1,Value2,Value3,Value4,Value5,Value6",
            CsvRow("Range", "From", dashboard.Range.From.ToString("yyyy-MM-dd")),
            CsvRow("Range", "To", dashboard.Range.To.ToString("yyyy-MM-dd")),
            CsvRow("Range", "Preset", dashboard.Range.Preset ?? string.Empty),
            CsvRow("Range", "Granularity", dashboard.Range.Granularity),
            CsvRow("Range", "Timezone", dashboard.Range.Timezone),
            CsvRow("Range", "ComparisonFrom", dashboard.Range.ComparisonFrom.ToString("yyyy-MM-dd")),
            CsvRow("Range", "ComparisonTo", dashboard.Range.ComparisonTo.ToString("yyyy-MM-dd")),

            CsvRow("SummaryCard", "TotalRevenue", dashboard.Cards.TotalRevenue.Value, dashboard.Cards.TotalRevenue.PreviousValue, dashboard.Cards.TotalRevenue.ChangePercent, dashboard.Cards.TotalRevenue.ChangeDirection, dashboard.Cards.TotalRevenue.ComparisonSupported),
            CsvRow("SummaryCard", "TotalOrders", dashboard.Cards.TotalOrders.Value, dashboard.Cards.TotalOrders.PreviousValue, dashboard.Cards.TotalOrders.ChangePercent, dashboard.Cards.TotalOrders.ChangeDirection, dashboard.Cards.TotalOrders.ComparisonSupported),
            CsvRow("SummaryCard", "NewUsers", dashboard.Cards.NewUsers.Value, dashboard.Cards.NewUsers.PreviousValue, dashboard.Cards.NewUsers.ChangePercent, dashboard.Cards.NewUsers.ChangeDirection, dashboard.Cards.NewUsers.ComparisonSupported),
            CsvRow("SummaryCard", "ActiveProducts", dashboard.Cards.ActiveProducts.Value, dashboard.Cards.ActiveProducts.PreviousValue, dashboard.Cards.ActiveProducts.ChangePercent, dashboard.Cards.ActiveProducts.ChangeDirection, dashboard.Cards.ActiveProducts.ComparisonSupported),

            CsvRow("RevenueSummary", "Overview", dashboard.RevenueSeries.Summary.GrossRevenue, dashboard.RevenueSeries.Summary.RefundAmount, dashboard.RevenueSeries.Summary.NetRevenue, dashboard.RevenueSeries.Summary.PaidOrders, dashboard.RevenueSeries.Summary.AverageOrderValue, dashboard.RevenueSeries.Summary.Currency)
        };

        lines.AddRange(dashboard.RevenueSeries.Points.Select(point =>
            CsvRow("RevenuePoint", point.Label, point.BucketStart.ToString("yyyy-MM-dd"), point.BucketEnd.ToString("yyyy-MM-dd"), point.GrossRevenue, point.RefundAmount, point.NetRevenue, point.PaidOrders)));

        lines.Add(CsvRow("OrdersByStatus", "TotalOrders", dashboard.OrdersByStatus.TotalOrders));
        lines.AddRange(dashboard.OrdersByStatus.Items.Select(item =>
            CsvRow("OrdersByStatusItem", item.Status, item.Count, item.Percentage)));

        lines.AddRange(dashboard.TopProducts.Items.Select(item =>
            CsvRow("TopProduct", item.ProductName, item.ProductId, item.ThumbnailUrl ?? string.Empty, item.UnitsSold, item.OrderCount, item.GrossRevenue)));

        lines.Add(CsvRow("NewUsers", "TotalNewUsers", dashboard.NewUsersSeries.TotalNewUsers));
        lines.AddRange(dashboard.NewUsersSeries.Points.Select(point =>
            CsvRow("NewUserPoint", point.Label, point.BucketStart.ToString("yyyy-MM-dd"), point.BucketEnd.ToString("yyyy-MM-dd"), point.Users)));

        return string.Join(Environment.NewLine, lines);
    }

    private static string CsvRow(string section, string key, params object?[] values)
    {
        var cells = new[] { section, key }
            .Concat(values.Select(FormatCsvValue));

        return string.Join(",", cells);
    }

    private static string FormatCsvValue(object? value)
    {
        var stringValue = value switch
        {
            null => string.Empty,
            bool boolValue => boolValue ? "true" : "false",
            IFormattable formattable => formattable.ToString(null, System.Globalization.CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty
        };

        if (stringValue.Contains(',') || stringValue.Contains('"') || stringValue.Contains('\n'))
        {
            return $"\"{stringValue.Replace("\"", "\"\"")}\"";
        }

        return stringValue;
    }
}
