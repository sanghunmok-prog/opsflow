# API Contract

The API currently exposes health, authentication, role-aware case read endpoints, basic Manager/Admin case creation, and an authenticated case type lookup for the Angular create form.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/health` | Confirms the API process is running. |
| POST | `/api/auth/login` | Issues a JWT bearer token for seeded demo users. |
| GET | `/api/auth/me` | Returns the authenticated user profile and roles. |
| GET | `/api/cases` | Returns a paginated, filtered, sorted, role-aware case queue. |
| GET | `/api/cases/{id}` | Returns accessible case detail by id. |
| POST | `/api/cases` | Creates a new unassigned case as Manager/Admin. |
| GET | `/api/case-types` | Returns active case type id/name pairs for dropdown lookup. |

PR-06 implements the Angular case queue screen and simple Manager/Admin create form. Notes, assignment, status transition, approval, audit timeline endpoints, dashboard endpoints, case detail UI, and admin configuration remain later PR scope.

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

## Planned Contract Areas

- Notes and status workflow
- Manager reassignment
- Manager approval decisions
- Audit timeline
- SQL-backed dashboard metrics

## Error Handling Placeholder

Case query and creation validation currently return simple `400` responses. Authorization failures use standard `401`/`403` behavior. Broader Problem Details and concurrency response documentation will be expanded when those behaviors are implemented.

## Current Boundary

No notes, assignment mutation, status transition, approval, dashboard, export, notification, case detail UI, or case type administration endpoints are implemented in PR-06.
