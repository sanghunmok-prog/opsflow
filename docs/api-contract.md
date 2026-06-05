# API Contract

OpsFlow API enforces authentication, role/object authorization, workflow transitions, SLA calculation, RowVersion concurrency checks, business audit timeline writes, and SQL-backed dashboard metrics.

Base local API URL: `http://localhost:5080`.

## Endpoint Summary

| Method | Route | Purpose | Authorization |
| --- | --- | --- | --- |
| `GET` | `/health` | Health check | Anonymous |
| `POST` | `/api/auth/login` | Issue JWT | Anonymous |
| `GET` | `/api/auth/me` | Current user | Authenticated |
| `GET` | `/api/cases` | Case queue | Analyst, Manager, Admin |
| `POST` | `/api/cases` | Create case | Manager, Admin |
| `GET` | `/api/cases/{caseId}` | Case detail | Analyst assigned to case, Manager, Admin |
| `PATCH` | `/api/cases/{caseId}/assign` | Assign/reassign | Manager, Admin |
| `PATCH` | `/api/cases/{caseId}/status` | Status transition | Analyst assigned to case, Manager, Admin |
| `POST` | `/api/cases/{caseId}/closure-request` | Request closure approval | Analyst assigned to case, Manager, Admin |
| `GET` | `/api/cases/{caseId}/notes` | List notes | Analyst assigned to case, Manager, Admin |
| `POST` | `/api/cases/{caseId}/notes` | Add note | Analyst assigned to case, Manager, Admin |
| `GET` | `/api/cases/{caseId}/timeline` | Timeline | Analyst assigned to case, Manager, Admin |
| `GET` | `/api/approvals/pending` | Pending approvals | Manager, Admin |
| `POST` | `/api/approvals/{approvalId}/approve` | Approve closure | Manager, Admin |
| `POST` | `/api/approvals/{approvalId}/reject` | Reject closure | Manager, Admin |
| `GET` | `/api/dashboard/summary` | Dashboard summary | Analyst, Manager, Admin |
| `GET` | `/api/dashboard/breakdowns` | Dashboard breakdowns | Analyst, Manager, Admin |
| `GET` | `/api/case-types` | Active case types | Authenticated |
| `GET` | `/api/users/analysts` | Active Analysts | Manager, Admin |

## Common DTOs

`PagedResult<T>`:

```json
{
  "items": [],
  "page": 1,
  "pageSize": 20,
  "totalCount": 0,
  "totalPages": 0
}
```

`UserSummaryDto`:

```json
{ "id": "guid", "displayName": "Morgan Manager" }
```

`CaseTypeSummaryDto`:

```json
{ "id": "guid", "name": "Vendor Approval Issue" }
```

`CaseListItemDto`:

```json
{
  "id": "guid",
  "caseNumber": "OPF-2026-0046",
  "title": "Vendor Approval Issue for customer account #0046",
  "caseType": { "id": "guid", "name": "Vendor Approval Issue" },
  "priority": "High",
  "status": "PendingApproval",
  "assignedTo": { "id": "guid", "displayName": "Alex Analyst" },
  "createdAtUtc": "2026-06-04T09:00:00Z",
  "dueAtUtc": "2026-06-05T09:00:00Z",
  "isOverdue": false
}
```

`CaseDetailDto`:

```json
{
  "id": "guid",
  "caseNumber": "OPF-2026-0046",
  "title": "Vendor Approval Issue for customer account #0046",
  "description": "Operational exception details.",
  "caseType": { "id": "guid", "name": "Vendor Approval Issue" },
  "priority": "High",
  "status": "PendingApproval",
  "assignedTo": { "id": "guid", "displayName": "Alex Analyst" },
  "createdBy": { "id": "guid", "displayName": "Morgan Manager" },
  "createdAtUtc": "2026-06-04T09:00:00Z",
  "updatedAtUtc": "2026-06-04T13:00:00Z",
  "dueAtUtc": "2026-06-05T09:00:00Z",
  "closedAtUtc": null,
  "isOverdue": false,
  "rowVersion": "AAAAAAAAB9E=",
  "approvalSummary": {
    "approvalId": "guid",
    "status": "Pending",
    "requestReason": "Work is complete.",
    "requestedBy": { "id": "guid", "displayName": "Alex Analyst" },
    "requestedAtUtc": "2026-06-04T13:00:00Z",
    "decisionReason": null,
    "reviewedBy": null,
    "decisionAtUtc": null
  }
}
```

`ApprovalQueueItemDto`:

```json
{
  "id": "guid",
  "caseId": "guid",
  "caseNumber": "OPF-2026-0046",
  "caseTitle": "Vendor Approval Issue for customer account #0046",
  "priority": "High",
  "caseStatus": "PendingApproval",
  "requestReason": "Work is complete.",
  "requestedBy": { "id": "guid", "displayName": "Alex Analyst" },
  "requestedAtUtc": "2026-06-04T13:00:00Z",
  "assignedTo": { "id": "guid", "displayName": "Alex Analyst" },
  "dueAtUtc": "2026-06-05T09:00:00Z",
  "isOverdue": false,
  "rowVersion": "AAAAAAAAB9E="
}
```

`DashboardSummaryDto`:

```json
{
  "openCases": 42,
  "overdueOpenCases": 7,
  "pendingApprovals": 3,
  "averageOpenAgeHours": 58.4,
  "slaBreachRate": 0.1667
}
```

`DashboardBreakdownsDto`:

```json
{
  "byStatus": [
    { "key": "PendingApproval", "label": "PendingApproval", "count": 3, "routeQuery": { "status": "PendingApproval" } }
  ],
  "byPriority": [
    { "key": "High", "label": "High", "count": 9, "routeQuery": { "priority": "High" } }
  ],
  "byCaseType": [
    { "key": "guid", "label": "Vendor Approval Issue", "count": 5, "routeQuery": { "caseTypeId": "guid" } }
  ],
  "byAssignee": [
    { "key": "guid", "label": "Alex Analyst", "count": 8, "routeQuery": { "assignedToUserId": "guid" } }
  ]
}
```

`TimelineEventDto`:

```json
{
  "id": "guid",
  "action": "ClosureRequested",
  "actor": { "id": "guid", "displayName": "Alex Analyst" },
  "createdAtUtc": "2026-06-04T13:00:00Z",
  "description": "Closure approval requested: Work is complete."
}
```

Handled application errors generally use:

```json
{ "message": "This case was updated by another user. Please refresh." }
```

ASP.NET Core model binding and malformed JSON can return framework validation/problem responses.

## Error Handling

| Status | Meaning |
| --- | --- |
| `400` | Invalid request body, invalid enum/query value, missing required command field, invalid `rowVersion` format. |
| `401` | Missing/invalid authentication or invalid login. |
| `403` | Role policy or object-level authorization failure. |
| `404` | Requested case, approval, or case type does not exist. |
| `409` | Stale `RowVersion`, duplicate pending approval, already-decided approval, or related case state conflict. |
| `422` | Syntactically valid request rejected by status workflow/domain rule; also used when case creation has no active SLA rule. |

Examples:

```json
{ "message": "Invalid targetStatus." }
```

```json
{ "message": "High and Critical case closure requires the approval workflow." }
```

```json
{ "message": "This case already has a pending approval request." }
```

`Forbid()` responses may have an empty body. Several not-found paths also return an empty body.

## RowVersion Policy

| Endpoint | Required | Missing | Invalid format | Stale supplied value | Success response |
| --- | --- | --- | --- | --- | --- |
| `PATCH /api/cases/{caseId}/assign` | No | Allowed | `400` | `409` | `CaseDetailDto` with fresh `rowVersion` |
| `PATCH /api/cases/{caseId}/status` | Yes | `400` | `400` | `409` | `CaseDetailDto` with fresh `rowVersion` |
| `POST /api/cases/{caseId}/closure-request` | Yes | `400` | `400` | `409` | `ApprovalRequestDto` with fresh case `rowVersion` |
| `POST /api/approvals/{approvalId}/approve` | No | Allowed | `400` | `409` | `ApprovalDecisionResultDto` with fresh case `rowVersion` |
| `POST /api/approvals/{approvalId}/reject` | No | Allowed | `400` | `409` | `ApprovalDecisionResultDto` with fresh case `rowVersion` |

Approval decisions also require a pending approval request and a related case currently in `PendingApproval`.

## Case List

`GET /api/cases`

Query parameters:

- `page` default `1`; must be `>= 1`.
- `pageSize` default `20`; allowed `1..100`.
- `search` matches case number, title, or description.
- `status` accepts `New`, `Assigned`, `InReview`, `WaitingInfo`, `Resolved`, `PendingApproval`, `Closed`, `Reopened`.
- `priority` accepts `Critical`, `High`, `Medium`, `Low`.
- `caseTypeId` filters by case type.
- `assignedToUserId` filters by assignee for Manager/Admin.
- `overdue=true|false` filters by query-time overdue formula.
- `sortBy` accepts `caseNumber`, `createdAtUtc`, `dueAtUtc`, `priority`, `status`.
- `sortDirection` accepts `asc` or `desc`; default is `desc`.

Analysts are always server-scoped to the current user's assigned cases. If an Analyst supplies a different `assignedToUserId`, the API returns `403`.

## Case Detail

`GET /api/cases/{caseId}` returns the latest case representation and `rowVersion`. It enforces the same access rule as the queue: assigned Analyst, Manager, or Admin. `isOverdue` is derived at query time from `Status != Closed && nowUtc > DueAtUtc`.

## Case Create

`POST /api/cases` is Manager/Admin-only.

Request:

```json
{
  "title": "Vendor approval missing",
  "description": "Approval record is incomplete.",
  "caseTypeId": "guid",
  "priority": "High"
}
```

Rules:

- `title`, `caseTypeId`, and `priority` are required.
- `description` is accepted up to 4000 characters.
- `priority` must be a named enum value, not a numeric enum value.
- New case starts `New` and unassigned.
- `caseNumber` is generated.
- `DueAtUtc` is calculated from the active SLA rule for `CaseTypeId + Priority`.
- `CaseCreated` audit event is written.
- `CreatedAtUtc` and `UpdatedAtUtc` are set to current UTC time.
- Missing case type returns `404`.
- Missing active SLA rule returns `422`.
- Success returns `201 Created` with `CaseDetailDto`.

## Assignment

`PATCH /api/cases/{caseId}/assign` is Manager/Admin-only.

Request:

```json
{
  "assignedToUserId": "guid",
  "reason": "Routing to the owning analyst.",
  "rowVersion": "AAAAAAAAB9E="
}
```

Rules:

- Target user must be an active Analyst.
- `reason` is required and limited to 500 characters.
- Closed cases are rejected.
- Same-assignee requests are rejected.
- `rowVersion` is optional; stale supplied values return `409`.
- If the case is `New`, assignment also moves it to `Assigned`.
- Writes `AssignmentHistory`, `Assigned` audit event, and status history for `New -> Assigned`.
- Updates `UpdatedAtUtc`.
- The implementation allows reassignment for non-closed states, including `Resolved` and `PendingApproval`, subject to Manager/Admin role and other validation.

## Status Transition

`PATCH /api/cases/{caseId}/status`

Request:

```json
{
  "targetStatus": "Resolved",
  "reason": "Investigation complete.",
  "rowVersion": "AAAAAAAAB9E="
}
```

Rules:

- `targetStatus`, `reason`, and `rowVersion` are required.
- Direct `PendingApproval` is rejected with `422`.
- Stale `rowVersion` returns `409`.
- Assigned Analysts can use only their assigned-case transitions.
- Managers/Admins can use the Manager/Admin transition set.

Allowed normal status transitions:

| From | To | Analyst | Manager/Admin |
| --- | --- | --- | --- |
| `Assigned` | `InReview`, `WaitingInfo` | Yes, if assigned | Yes |
| `InReview` | `WaitingInfo`, `Resolved` | Yes, if assigned | Yes |
| `WaitingInfo` | `InReview`, `Resolved` | Yes, if assigned | Yes |
| `Resolved` | `Closed` | No | Low/Medium only |
| `Closed` | `Reopened` | No | Yes |
| `Reopened` | `InReview` | Yes, if assigned | Yes |
| `Reopened` | `WaitingInfo` | No | Yes |

Timestamp side effects:

- Target `Resolved` sets `ResolvedAtUtc` if unset.
- Target `Closed` sets `ClosedAtUtc`.
- Target `Reopened` clears `ClosedAtUtc`.
- Every accepted transition updates `UpdatedAtUtc`.

## Closure Request

`POST /api/cases/{caseId}/closure-request`

Request:

```json
{
  "requestReason": "High-priority work is resolved and ready for closure.",
  "rowVersion": "AAAAAAAAB9E="
}
```

Rules:

- Allowed only for `Resolved` High/Critical cases.
- Assigned Analyst can request for own assigned case.
- Manager/Admin can request globally.
- Low/Medium cases are rejected because they close through normal status workflow.
- Non-`Resolved` cases are rejected.
- Duplicate pending approval returns `409`.
- `rowVersion` is required; stale values return `409`.
- Success sets case status to `PendingApproval`, creates `ApprovalRequest`, writes `StatusHistory`, writes `ClosureRequested`, updates `UpdatedAtUtc`, and returns `ApprovalRequestDto`.

## Pending Approvals

`GET /api/approvals/pending?page=1&pageSize=20` is Manager/Admin-only. `pageSize` is limited to `1..100`.

The response is `PagedResult<ApprovalQueueItemDto>` ordered by request time. Each item includes `approvalId`, `caseId`, case number/title, priority, current case status, request reason, requester, assignee, due date, query-time overdue flag, and latest case `rowVersion`.

## Approval Decisions

Approve:

`POST /api/approvals/{approvalId}/approve`

```json
{
  "decisionReason": "Closure approved.",
  "rowVersion": "AAAAAAAAB9E="
}
```

Rules:

- Manager/Admin-only.
- Pending approval request required.
- Related case must be `PendingApproval`.
- Case priority must be High/Critical.
- `decisionReason` is optional; blank is treated as null.
- Optional supplied `rowVersion` is checked for staleness.
- Marks approval `Approved`, sets reviewer and decision timestamp, moves case to `Closed`, sets `ClosedAtUtc`, writes status history, writes `ApprovalApproved`, and returns latest `rowVersion`.

Reject:

`POST /api/approvals/{approvalId}/reject`

```json
{
  "decisionReason": "Additional evidence is required.",
  "rowVersion": "AAAAAAAAB9E="
}
```

Rules:

- Manager/Admin-only.
- Pending approval request required.
- Related case must be `PendingApproval`.
- Case priority must be High/Critical.
- `decisionReason` is required.
- Optional supplied `rowVersion` is checked for staleness.
- Marks approval `Rejected`, sets reviewer and decision timestamp, moves case to `InReview`, leaves `ClosedAtUtc` unset, writes status history, writes `ApprovalRejected`, and returns latest `rowVersion`.

Conflict cases include already-decided approval, stale supplied `RowVersion`, duplicate pending approval, and related case not `PendingApproval`.

## Notes And Timeline

Notes use the same object-level access as case detail.

`POST /api/cases/{caseId}/notes` request:

```json
{ "body": "Follow-up captured for the operations team." }
```

The body is trimmed, required, limited to 2000 characters, and stored as plain text. Adding a note writes `NoteAdded`.

Timeline events are returned in chronological order and include actor display, timestamp, action, and description. Supported actions are `CaseCreated`, `NoteAdded`, `Assigned`, `StatusChanged`, `ClosureRequested`, `ApprovalApproved`, `ApprovalRejected`, and `CaseReopened`.

## Dashboard

Dashboard endpoints are role-aware:

- Manager/Admin metrics use all cases and all pending approvals.
- Analyst metrics use assigned cases and pending approvals for assigned cases.

Summary formulae:

- `openCases`: scoped cases where `Status != Closed`.
- `overdueOpenCases`: open scoped cases where `DueAtUtc < nowUtc`.
- `pendingApprovals`: pending approval requests in role scope.
- `averageOpenAgeHours`: average `(nowUtc - CreatedAtUtc)` over open scoped cases.
- `slaBreachRate`: `overdueOpenCases / openCases`, or `0` when no open cases exist.

Breakdowns group scoped cases by status, priority, case type, and assignee. Each item may include route query values used by the Angular queue drill-down. There is no stored summary table or background metrics job.

## Lookups

- `GET /api/case-types` returns active case types and is read-only.
- `GET /api/users/analysts` returns active Analyst users and is Manager/Admin-only.
- No mutation endpoints are exposed for users, roles, case types, or SLA rules.

## Current Boundary

The API does not expose user administration, role administration, case type mutation, SLA rule mutation, note edit/delete, file attachment, export/report builder, notification, worker-control, or real-time messaging endpoints.
