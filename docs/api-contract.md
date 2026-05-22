# API Contract

The API currently exposes health, authentication, and role-aware case read endpoints.

| Method | Route | Purpose |
| --- | --- | --- |
| GET | `/health` | Confirms the API process is running. |
| POST | `/api/auth/login` | Issues a JWT bearer token for seeded demo users. |
| GET | `/api/auth/me` | Returns the authenticated user profile and roles. |
| GET | `/api/cases` | Returns a paginated, filtered, sorted, role-aware case queue. |
| GET | `/api/cases/{id}` | Returns accessible case detail by id. |

PR-03 implements backend case read APIs only. Workflow mutations, notes, assignment, approval, audit timeline, dashboard endpoints, and Angular business screens remain later PR scope.

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
- `sortBy`: `caseNumber`, `createdAtUtc`, `dueAtUtc`, `priority`, or `status`
- `sortDirection`: `asc` or `desc`

Analysts are always constrained to `AssignedToUserId == currentUserId`. Managers and Admins can read all matching cases.

## Case Detail

`GET /api/cases/{id}` requires authentication.

- Missing token: `401`
- Missing case id: `404`
- Existing case outside Analyst assignment scope: `403`
- Accessible case: `200`

Detail responses include case metadata, case type summary, assigned user summary, created-by user summary, timestamps, due date, and base64 row version.

## Planned Contract Areas

- SLA due date and overdue indicators
- Notes and status workflow
- Manager reassignment
- Manager approval decisions
- Audit timeline
- SQL-backed dashboard metrics

## Error Handling Placeholder

Case query validation currently returns simple `400` responses. Authorization failures use standard `401`/`403` behavior. Broader Problem Details and concurrency response documentation will be expanded when those behaviors are implemented.

## Current Boundary

No case creation, notes, assignment mutation, status transition, approval, dashboard, export, notification, or Angular business UI endpoints are implemented in PR-03.
