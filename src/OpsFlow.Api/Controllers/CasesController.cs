using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpsFlow.Application.Approvals;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;

namespace OpsFlow.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/cases")]
public sealed class CasesController(
    ICaseQueryService caseQueryService,
    ICaseCommandService caseCommandService,
    ICaseAssignmentService caseAssignmentService,
    ICaseStatusService caseStatusService,
    ICaseNoteService caseNoteService,
    ICaseTimelineService caseTimelineService,
    IApprovalService approvalService) : ControllerBase
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

    [HttpPost]
    [Authorize(Policy = OpsFlowPolicies.RequireManagerOrAdmin)]
    public async Task<ActionResult<CaseDetailDto>> CreateCase(
        [FromBody] CreateCaseRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var created = await caseCommandService.CreateCaseAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetCase), new { id = created.Id }, created);
        }
        catch (CaseCommandValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CaseTypeNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (SlaRuleNotFoundException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
    }

    [HttpPatch("{caseId:guid}/assign")]
    [Authorize(Policy = OpsFlowPolicies.RequireManagerOrAdmin)]
    public async Task<ActionResult<CaseDetailDto>> AssignCase(
        Guid caseId,
        [FromBody] AssignCaseRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await caseAssignmentService.AssignCaseAsync(caseId, request, cancellationToken));
        }
        catch (CaseNotFoundException)
        {
            return NotFound();
        }
        catch (CaseAssignmentValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CaseAssignmentConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpPatch("{caseId:guid}/status")]
    public async Task<ActionResult<CaseDetailDto>> UpdateStatus(
        Guid caseId,
        [FromBody] UpdateCaseStatusRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await caseStatusService.UpdateStatusAsync(caseId, request, cancellationToken));
        }
        catch (CaseNotFoundException)
        {
            return NotFound();
        }
        catch (CaseStatusValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CaseStatusTransitionException ex)
        {
            return UnprocessableEntity(new { message = ex.Message });
        }
        catch (CaseStatusConcurrencyException ex)
        {
            return Conflict(new { message = ex.Message });
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpPost("{caseId:guid}/closure-request")]
    public async Task<ActionResult<ApprovalRequestDto>> RequestClosure(
        Guid caseId,
        [FromBody] RequestClosureRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await approvalService.RequestClosureAsync(caseId, request, cancellationToken));
        }
        catch (CaseNotFoundException)
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

    [HttpGet("{caseId:guid}/notes")]
    public async Task<ActionResult<IReadOnlyList<CaseNoteDto>>> GetNotes(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        try
        {
            var notes = await caseNoteService.GetNotesAsync(caseId, cancellationToken);
            return notes is null ? NotFound() : Ok(notes);
        }
        catch (CaseNotFoundException)
        {
            return NotFound();
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpPost("{caseId:guid}/notes")]
    public async Task<ActionResult<CaseNoteDto>> AddNote(
        Guid caseId,
        [FromBody] CreateCaseNoteRequest? request,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await caseNoteService.AddNoteAsync(caseId, request, cancellationToken);
            return note is null
                ? NotFound()
                : Created($"/api/cases/{caseId}/notes/{note.Id}", note);
        }
        catch (CaseNoteValidationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (CaseNotFoundException)
        {
            return NotFound();
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }

    [HttpGet("{caseId:guid}/timeline")]
    public async Task<ActionResult<IReadOnlyList<CaseTimelineItemDto>>> GetTimeline(
        Guid caseId,
        CancellationToken cancellationToken)
    {
        try
        {
            var timeline = await caseTimelineService.GetTimelineAsync(caseId, cancellationToken);
            return timeline is null ? NotFound() : Ok(timeline);
        }
        catch (CaseAccessDeniedException)
        {
            return Forbid();
        }
    }
}
