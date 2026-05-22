namespace MoriiCoffee.Application.SeedWork.DTOs.Report;

/// <summary>
/// Full dashboard response returned by the admin reports endpoint.
/// </summary>
public class AdminReportsDashboardDto
{
    /// <summary>Normalized reporting period metadata.</summary>
    public ReportRangeDto Range { get; set; } = new();

    /// <summary>Summary cards shown in the reports header.</summary>
    public AdminReportsCardsDto Cards { get; set; } = new();

    /// <summary>Revenue trend section.</summary>
    public RevenueSeriesDto RevenueSeries { get; set; } = new();

    /// <summary>Order-status breakdown section.</summary>
    public OrderStatusBreakdownDto OrdersByStatus { get; set; } = new();

    /// <summary>Top-products section.</summary>
    public AdminReportsTopProductsDto TopProducts { get; set; } = new();

    /// <summary>New-user growth section.</summary>
    public NewUsersSeriesDto NewUsersSeries { get; set; } = new();
}

/// <summary>
/// Collection of summary cards returned by the dashboard.
/// </summary>
public class AdminReportsCardsDto
{
    /// <summary>Total revenue card.</summary>
    public ReportMetricCardDto TotalRevenue { get; set; } = new();

    /// <summary>Total orders card.</summary>
    public ReportMetricCardDto TotalOrders { get; set; } = new();

    /// <summary>New users card.</summary>
    public ReportMetricCardDto NewUsers { get; set; } = new();

    /// <summary>Active products card.</summary>
    public ReportMetricCardDto ActiveProducts { get; set; } = new();
}

/// <summary>
/// Wrapper for the top-products collection.
/// </summary>
public class AdminReportsTopProductsDto
{
    /// <summary>Ranked products for the selected range.</summary>
    public List<TopProductDto> Items { get; set; } = [];
}
