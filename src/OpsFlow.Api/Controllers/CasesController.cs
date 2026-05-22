using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Cases;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cases")]
public sealed class CasesController(ICaseQueryService caseQueryService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PagedResult<CaseListItemDto>>> GetCases(
        [FromQuery] CaseListQuery query,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await caseQueryService.GetCasesAsync(query, cancellationToken));
        }
        catch (CaseQueryValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<CaseDetailDto>> GetCase(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var opsCase = await caseQueryService.GetCaseAsync(id, cancellationToken);
            return opsCase is null ? NotFound() : Ok(opsCase);
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }
}
