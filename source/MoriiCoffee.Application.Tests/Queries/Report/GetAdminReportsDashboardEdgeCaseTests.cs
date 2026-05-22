using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Report.GetAdminReportsDashboard;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.Services.Reports;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.Report;

public class GetAdminReportsDashboardEdgeCaseTests
{
    [Fact]
    public async Task Handle_ZeroBaselineAndZeroActivity_ArePreservedInResponse()
    {
        var unitOfWork = new Mock<IUnitOfWork>();
        var reportsRepository = new Mock<IAdminReportsReadRepository>();
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTime(2026, 05, 22, 14, 0, 0, DateTimeKind.Utc));

        reportsRepository.Setup(x => x.GetDashboardAsync(It.IsAny<AdminReportQueryRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReportReadModelFactory.CreateZeroActivity());
        unitOfWork.Setup(x => x.AdminReports).Returns(reportsRepository.Object);

        var handler = new GetAdminReportsDashboardQueryHandler(
            new ReportQueryNormalizer(dateTimeProvider.Object, new ComparisonPeriodResolver()),
            unitOfWork.Object);

        var result = await handler.Handle(
            new GetAdminReportsDashboardQuery("30D", null, null, "day", "Asia/Ho_Chi_Minh"),
            CancellationToken.None);

        result.Cards.TotalRevenue.PreviousValue.Should().Be(0m);
        result.Cards.TotalRevenue.ChangePercent.Should().BeNull();
        result.Cards.TotalRevenue.ChangeDirection.Should().Be("up_from_zero");
        result.RevenueSeries.Points.Should().ContainSingle(x => x.NetRevenue == 0m);
        result.OrdersByStatus.TotalOrders.Should().Be(0);
        result.NewUsersSeries.TotalNewUsers.Should().Be(0);
    }
}
