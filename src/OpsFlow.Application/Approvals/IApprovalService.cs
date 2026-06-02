using OpsFlow.Application.Cases;

namespace OpsFlow.Application.Approvals;

public interface IApprovalService
{
    Task<ApprovalRequestDto> RequestClosureAsync(
        Guid caseId,
        RequestClosureRequest? request,
        CancellationToken cancellationToken = default);

    Task<PagedResult<ApprovalQueueItemDto>> GetPendingApprovalsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    Task<ApprovalDecisionResultDto> ApproveAsync(
        Guid approvalId,
        ApprovalDecisionRequest? request,
        CancellationToken cancellationToken = default);

    Task<ApprovalDecisionResultDto> RejectAsync(
        Guid approvalId,
        ApprovalDecisionRequest? request,
        CancellationToken cancellationToken = default);
}
