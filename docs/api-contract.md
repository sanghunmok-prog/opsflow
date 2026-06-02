# API Contract

The API currently exposes health, authentication, role-aware case read endpoints, basic Manager/Admin case creation, Manager/Admin case assignment, role-aware case status transitions, case notes, a basic business timeline, an authenticated case type lookup, and a Manager/Admin active Analyst lookup.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/health` | Confirms the API process is running. |
| POST | `/api/auth/login` | Issues a JWT bearer token for seeded demo users. |
| GET | `/api/auth/me` | Returns the authenticated user profile and roles. |
| GET | `/api/cases` | Returns a paginated, filtered, sorted, role-aware case queue. |
| GET | `/api/cases/{id}` | Returns accessible case detail by id. |
| POST | `/api/cases` | Creates a new unassigned case as Manager/Admin. |
| PATCH | `/api/cases/{caseId}/assign` | Assigns or reassigns a case to an active Analyst as Manager/Admin. |
| PATCH | `/api/cases/{caseId}/status` | Updates case status through the PR-09 transition matrix. |
| GET | `/api/cases/{caseId}/notes` | Returns notes for an accessible case. |
| POST | `/api/cases/{caseId}/notes` | Adds a plain text note to an accessible case. |
| GET | `/api/cases/{caseId}/timeline` | Returns basic business audit timeline events for an accessible case. |
| GET | `/api/case-types` | Returns active case type id/name pairs for dropdown lookup. |
| GET | `/api/users/analysts` | Returns active Analysts for the assignment dropdown as Manager/Admin. |

PR-09 implements status transitions with history, business audit, and RowVersion concurrency. Approval workflow, dashboard endpoints, notifications, and admin configuration remain later PR scope.

## Case List

`GET /api/cases` requires authentication.

Supported query parameters:

- `page`: default `1`, must be at least `1`
- `pageSize`: default `20`, must be between `1` and `100`
- `search`: trims input and matches case number, title, or description
- `status`: `New`, `Assigned`, `InReview`, `WaitingInfo`, `Resolved`, `PendingApproval`, `Closed`, or `Reopened`
- `priority`: `Critical`, `High`, `Medium`, or `Low`
- `caseTypeId`: case type id
- `assignedToUserId`: Manager/Admin only unless it matches the current Analyst user id
- `overdue`: `true` for open overdue cases, `false` for cases that are not overdue
- `sortBy`: `caseNumber`, `createdAtUtc`, `dueAtUtc`, `priority`, or `status`
- `sortDirection`: `asc` or `desc`

Analysts are always constrained to `AssignedToUserId == currentUserId`. Managers and Admins can read all matching cases.

List items include query-time `isOverdue`, calculated as `Status != Closed && nowUtc > dueAtUtc`.

## Case Detail

`GET /api/cases/{id}` requires authentication.

- Missing token: `401`
- Missing case id: `404`
- Existing case outside Analyst assignment scope: `403`
- Accessible case: `200`

Detail responses include case metadata, case type summary, assigned user summary, created-by user summary, timestamps, due date, query-time `isOverdue`, and base64 row version.

## Case Notes

`GET /api/cases/{caseId}/notes` and `POST /api/cases/{caseId}/notes` require authentication and enforce the same object-level access rules as case detail.

- Missing token: `401`
- Missing case id: `404`
- Existing case outside Analyst assignment scope: `403`
- Accessible case: `200` for reads, `201` for successful note creation

GET response body:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "body": "Reviewed the case and confirmed next action.",
    "createdBy": {
      "id": "00000000-0000-0000-0000-000000000000",
      "displayName": "Alex Analyst"
    },
    "createdAtUtc": "2026-05-22T14:30:00Z"
  }
]
```

POST request body:

```json
{
  "body": "Reviewed the case and confirmed next action."
}
```

Note bodies are trimmed, required, plain text, and limited to 2000 characters. Empty or whitespace-only bodies return `400`. Successful creation writes both a `CaseNote` row and a `NoteAdded` business audit row.

## Case Timeline

`GET /api/cases/{caseId}/timeline` requires authentication and enforces the same object-level access rules as case detail.

- Missing token: `401`
- Missing case id: `404`
- Existing case outside Analyst assignment scope: `403`
- Accessible case: `200`

Response body:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "action": "CaseCreated",
    "actor": {
      "id": "00000000-0000-0000-0000-000000000000",
      "displayName": "Morgan Manager"
    },
    "createdAtUtc": "2026-05-22T14:00:00Z",
    "description": "Case created"
  }
]
```

Timeline output is ordered by `createdAtUtc` ascending and includes `CaseCreated`, `NoteAdded`, `Assigned`, `StatusChanged`, and `CaseReopened` audit events.

Assigned events use simple descriptions such as:

- `Assigned to Alex Analyst`
- `Reassigned from Alex Analyst to Blair Analyst`

Status events use simple descriptions such as:

- `Status changed from Assigned to InReview`
- `Case reopened`

## Case Create

`POST /api/cases` requires the Manager or Admin role.

Request body:

```json
{
  "title": "Vendor onboarding exception",
  "description": "Synthetic internal operations case.",
  "caseTypeId": "00000000-0000-0000-0000-000000000000",
  "priority": "High"
}
```

Creation behavior:

- Missing token: `401`
- Analyst token: `403`
- Invalid request: `400`
- Missing case type: `404`
- Missing active SLA rule: `422`
- Success: `201`

The API sets `Status = New`, leaves `AssignedTo = null`, records `CreatedBy` from the authenticated user, generates an `OPF-YYYY-####` case number, calculates `DueAtUtc` from the active SLA rule, and writes a `CaseCreated` business audit row.

## Case Assignment

`PATCH /api/cases/{caseId}/assign` requires the Manager or Admin role.

Request body:

```json
{
  "assignedToUserId": "00000000-0000-0000-0000-000000000000",
  "reason": "Assigned for analyst review.",
  "rowVersion": "base64-row-version"
}
```

`rowVersion` may be supplied by clients but PR-08 does not enforce optimistic concurrency for assignment.

Behavior:

- Missing token: `401`
- Analyst token: `403`
- Missing case id: `404`
- Missing assignee, empty reason, inactive/non-Analyst target, same assignee, or closed case: `400`
- Success: `200` with refreshed case detail

Successful assignment sets `AssignedToUserId`, updates `UpdatedAtUtc`, writes an `AssignmentHistory` row, and writes an `Assigned` business audit row. If the case was `New`, assignment changes status to `Assigned` and writes a `StatusHistory` row for `New -> Assigned`. Other statuses are not changed.

## Case Status Transition

`PATCH /api/cases/{caseId}/status` requires authentication.

Request body:

```json
{
  "targetStatus": "InReview",
  "reason": "Started review after assignment.",
  "rowVersion": "base64-row-version"
}
```

Behavior:

- Missing token: `401`
- Analyst outside assignment scope: `403`
- Missing case id: `404`
- Missing target status, empty reason, missing/invalid row version, or same status: `400`
- Stale row version: `409`
- Disallowed business transition: `422`
- Success: `200` with refreshed case detail and new `rowVersion`

Analysts can transition only their assigned cases through allowed non-close/non-reopen workflow steps. Managers and Admins can transition any case according to the PR-09 matrix, including closing Low/Medium resolved cases and reopening closed cases.

High/Critical `Resolved -> Closed` is blocked until the PR-10 approval workflow. PR-09 does not create `ApprovalRequest` rows, does not create `PendingApproval`, and does not expose approval endpoints.

Successful status transitions update `Cases.Status`, write `StatusHistory`, and write a business audit event: `StatusChanged` for normal status changes or `CaseReopened` for `Closed -> Reopened`.

## Case Type Lookup

`GET /api/case-types` requires authentication and returns active case types only.

Response body:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "name": "Vendor Approval Issue"
  }
]
```

No case type mutation endpoints are exposed.

## Analyst Lookup

`GET /api/users/analysts` requires the Manager or Admin role and returns active Analyst users only.

Response body:

```json
[
  {
    "id": "00000000-0000-0000-0000-000000000000",
    "displayName": "Alex Analyst",
    "email": "analyst1@opsflow.local"
  }
]
```

No user create, edit, delete, role management, or admin user configuration endpoints are exposed.

## Planned Contract Areas

- Manager approval decisions
- SQL-backed dashboard metrics

## Error Handling Placeholder

Case query and creation validation currently return simple `400` responses. Authorization failures use standard `401`/`403` behavior. Status stale-write detection returns `409`. Broader Problem Details documentation remains later scope.

## Current Boundary

No approval, dashboard, export, notification, note edit/delete, attachments, rich text, user management, role management, or case type administration endpoints are implemented in PR-09.
