using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Approvals;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Authorize(Policy = OpsFlowPolicies.RequireManagerOrAdmin)]
[Route("api/approvals")]
public sealed class ApprovalsController(IApprovalService approvalService) : ControllerBase
{
    [HttpGet("pending")]
    public async Task<ActionResult<PagedResult<ApprovalQueueItemDto>>> GetPendingApprovals(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await approvalService.GetPendingApprovalsAsync(page, pageSize, cancellationToken));
        }
        catch (ApprovalValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpPost("{approvalId:guid}/approve")]
    public async Task<ActionResult<ApprovalDecisionResultDto>> Approve(
        Guid approvalId,
        [FromBody] ApprovalDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await approvalService.ApproveAsync(approvalId, request, cancellationToken));
        }
        catch (ApprovalNotFoundException)
        {
            return NotFound();
        }
        catch (ApprovalValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ApprovalConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ApprovalStateConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpPost("{approvalId:guid}/reject")]
    public async Task<ActionResult<ApprovalDecisionResultDto>> Reject(
        Guid approvalId,
        [FromBody] ApprovalDecisionRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await approvalService.RejectAsync(approvalId, request, cancellationToken));
        }
        catch (ApprovalNotFoundException)
        {
            return NotFound();
        }
        catch (ApprovalValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (ApprovalConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (ApprovalStateConflictException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }
}
