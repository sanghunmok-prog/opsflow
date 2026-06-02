using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Users;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/users")]
public sealed class UsersController(IUserLookupService userLookupService) : ControllerBase
{
    [HttpGet("analysts")]
    [Authorize(Policy = OpsFlowPolicies.RequireManagerOrAdmin)]
    public async Task<ActionResult<IReadOnlyList<AnalystLookupDto>>> GetAnalysts(
        CancellationToken cancellationToken)
    {
        return Ok(await userLookupService.GetActiveAnalystsAsync(cancellationToken));
    }
}
