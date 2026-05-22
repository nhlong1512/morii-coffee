using MoriiCoffee.Application.SeedWork.DTOs.Report;
using MoriiCoffee.Application.Services.Reports;
using MoriiCoffee.Domain.SeedWork.Persistence;
using MoriiCoffee.Domain.SeedWork.Query;

namespace MoriiCoffee.Application.Queries.Report.GetAdminReportsDashboard;

/// <summary>
/// Loads the complete admin reports dashboard from the reporting read repository.
/// </summary>
public class GetAdminReportsDashboardQueryHandler : IQueryHandler<GetAdminReportsDashboardQuery, AdminReportsDashboardDto>
{
    private readonly ReportQueryNormalizer _reportQueryNormalizer;
    private readonly IUnitOfWork _unitOfWork;

    public GetAdminReportsDashboardQueryHandler(
        ReportQueryNormalizer reportQueryNormalizer,
        IUnitOfWork unitOfWork)
    {
        _reportQueryNormalizer = reportQueryNormalizer;
        _unitOfWork = unitOfWork;
    }

    public async Task<AdminReportsDashboardDto> Handle(
        GetAdminReportsDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var range = _reportQueryNormalizer.Normalize(
            request.Preset,
            request.From,
            request.To,
            request.Granularity,
            request.Timezone);

        var readModel = await _unitOfWork.AdminReports.GetDashboardAsync(range, cancellationToken);
        return ReportDtoMapper.ToDashboardDto(readModel);
    }
}
