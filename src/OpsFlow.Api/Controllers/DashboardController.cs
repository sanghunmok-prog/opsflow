using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;
using OpsFlow.Application.Dashboard;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = OpsFlowPolicies.RequireAnalystOrManagerOrAdmin)]
[Route("api/dashboard")]
public sealed class DashboardController(IDashboardQueryService dashboardQueryService) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummary(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await dashboardQueryService.GetSummaryAsync(cancellationToken));
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpGet("breakdowns")]
    public async Task<ActionResult<DashboardBreakdownsDto>> GetBreakdowns(CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await dashboardQueryService.GetBreakdownsAsync(cancellationToken));
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }
}
