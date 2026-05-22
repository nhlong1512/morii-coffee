using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Report.GetAdminReportsDashboard;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.Services.Reports;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.Shared.Enums.Order;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.Report;

public class GetAdminReportsDashboardQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAdminReportsReadRepository> _reportsRepository = new();
    private readonly GetAdminReportsDashboardQueryHandler _handler;

    public GetAdminReportsDashboardQueryHandlerTests()
    {
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTime(2026, 05, 22, 14, 0, 0, DateTimeKind.Utc));

        _unitOfWork.Setup(x => x.AdminReports).Returns(_reportsRepository.Object);

        _handler = new GetAdminReportsDashboardQueryHandler(
            new ReportQueryNormalizer(dateTimeProvider.Object, new ComparisonPeriodResolver()),
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_MapsReadModelIntoDashboardDto()
    {
        _reportsRepository
            .Setup(x => x.GetDashboardAsync(It.IsAny<AdminReportQueryRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReportReadModelFactory.Create());

        var result = await _handler.Handle(
            new GetAdminReportsDashboardQuery("30D", null, null, "day", "Asia/Ho_Chi_Minh"),
            CancellationToken.None);

        result.Range.Preset.Should().Be("30D");
        result.Cards.TotalRevenue.Value.Should().Be(1250000m);
        result.Cards.TotalRevenue.ChangeDirection.Should().Be("up");
        result.Cards.ActiveProducts.ComparisonSupported.Should().BeFalse();
        result.OrdersByStatus.Items.Should().ContainSingle(x => x.Status == EOrderStatus.DELIVERED.ToString() && x.Count == 7);
        result.TopProducts.Items.Should().ContainSingle(x => x.ProductName == "Iced Americano" && x.UnitsSold == 12);
        result.NewUsersSeries.Points.Should().ContainSingle(x => x.Label == "May 22" && x.Users == 3);
    }

}
