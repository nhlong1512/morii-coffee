using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MoriiCoffee.Application.Queries.Report.ExportAdminReports;
using MoriiCoffee.Application.Queries.Report.GetAdminReportsDashboard;
using MoriiCoffee.Application.SeedWork.DTOs.Report;
using MoriiCoffee.Domain.Shared.Constants;
using MoriiCoffee.Domain.Shared.Enums.User;
using MoriiCoffee.Domain.Shared.HttpResponses;
using Swashbuckle.AspNetCore.Annotations;

namespace MoriiCoffee.Presentation.Controllers;

/// <summary>
/// Internal admin reporting endpoints used by the admin dashboard and CSV export flow.
/// </summary>
[ApiController]
[Route("api/v1/admin/reports")]
[Authorize(Roles = nameof(ERole.ADMIN))]
public class AdminReportsController : ControllerBase
{
    private readonly IMediator _mediator;

    public AdminReportsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Returns the admin reports dashboard snapshot for the selected reporting period.
    /// </summary>
    [HttpGet("dashboard")]
    [Produces("application/json")]
    [SwaggerOperation(
        Summary = "Get admin reports dashboard",
        Description = "Returns summary cards, revenue trend, order-status breakdown, top products, and new-user growth for the selected reporting period.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully, typeof(AdminReportsDashboardDto))]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] string? preset,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? granularity,
        [FromQuery] string? timezone)
    {
        var result = await _mediator.Send(new GetAdminReportsDashboardQuery(preset, from, to, granularity, timezone));
        return Ok(new ApiOkResponse(result));
    }

    /// <summary>
    /// Exports the current admin reports view as CSV.
    /// </summary>
    [HttpGet("export")]
    [Produces("text/csv")]
    [SwaggerOperation(
        Summary = "Export admin reports CSV",
        Description = "Exports the same reporting period and analytical sections used by the admin dashboard as a CSV file.")]
    [SwaggerResponse(200, SwaggerResponseMessages.RetrievedSuccessfully)]
    public async Task<IActionResult> Export(
        [FromQuery] string? format,
        [FromQuery] string? preset,
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] string? granularity,
        [FromQuery] string? timezone)
    {
        var result = await _mediator.Send(new ExportAdminReportsQuery(format, preset, from, to, granularity, timezone));
        return File(result.Content, result.ContentType, result.FileName);
    }
}
