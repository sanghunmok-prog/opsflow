using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Cases;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/case-types")]
public sealed class CaseTypesController(OpsFlowDbContext dbContext) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CaseTypeLookupDto>>> GetCaseTypes(
        CancellationToken cancellationToken)
    {
        var caseTypes = await dbContext.CaseTypes
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new CaseTypeLookupDto(x.Id, x.Name))
            .ToListAsync(cancellationToken);

        return Ok(caseTypes);
    }
}
