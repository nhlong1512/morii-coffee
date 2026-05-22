using System.Text;
using FluentAssertions;
using Moq;
using MoriiCoffee.Application.Queries.Report.ExportAdminReports;
using MoriiCoffee.Application.SeedWork.Abstractions;
using MoriiCoffee.Application.SeedWork.Exceptions;
using MoriiCoffee.Application.Services.Reports;
using MoriiCoffee.Domain.Repositories;
using MoriiCoffee.Domain.SeedWork.Persistence;
using Xunit;

namespace MoriiCoffee.Application.Tests.Queries.Report;

public class ExportAdminReportsQueryHandlerTests
{
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IAdminReportsReadRepository> _reportsRepository = new();
    private readonly ExportAdminReportsQueryHandler _handler;

    public ExportAdminReportsQueryHandlerTests()
    {
        var dateTimeProvider = new Mock<IDateTimeProvider>();
        dateTimeProvider.Setup(x => x.UtcNow).Returns(new DateTime(2026, 05, 22, 14, 0, 0, DateTimeKind.Utc));

        _unitOfWork.Setup(x => x.AdminReports).Returns(_reportsRepository.Object);

        _handler = new ExportAdminReportsQueryHandler(
            new ReportQueryNormalizer(dateTimeProvider.Object, new ComparisonPeriodResolver()),
            _unitOfWork.Object);
    }

    [Fact]
    public async Task Handle_BuildsCsvContainingAllDashboardSections()
    {
        _reportsRepository
            .Setup(x => x.GetDashboardAsync(It.IsAny<AdminReportQueryRange>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ReportReadModelFactory.Create());

        var result = await _handler.Handle(
            new ExportAdminReportsQuery("csv", "30D", null, null, "day", "Asia/Ho_Chi_Minh"),
            CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.Content);

        result.ContentType.Should().Be("text/csv");
        result.FileName.Should().Be("admin-reports-20260424-20260522.csv");
        csv.Should().Contain("Section,Key,Value1,Value2,Value3,Value4,Value5,Value6");
        csv.Should().Contain("SummaryCard,TotalRevenue,1250000,1000000,25,up,true");
        csv.Should().Contain("RevenueSummary,Overview,1500000,250000,1250000,20,75000,VND");
        csv.Should().Contain("OrdersByStatusItem,DELIVERED,7,70");
        csv.Should().Contain("TopProduct,Iced Americano");
        csv.Should().Contain("NewUserPoint,May 22,2026-05-22,2026-05-22,3");
    }

    [Fact]
    public async Task Handle_UnsupportedFormat_ThrowsBadRequest()
    {
        var act = () => _handler.Handle(
            new ExportAdminReportsQuery("xlsx", "30D", null, null, "day", "Asia/Ho_Chi_Minh"),
            CancellationToken.None);

        await act.Should().ThrowAsync<BadRequestException>()
            .WithMessage("*csv export format*");
    }
}
