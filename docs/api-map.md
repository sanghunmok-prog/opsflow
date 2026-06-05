# API Map

This map summarizes the implemented HTTP surface. Route parameters use `caseId` and `approvalId` consistently.

## Auth

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `POST` | `/api/auth/login` | Issue JWT for active Identity user | Anonymous | Validates email/password and active user. |
| `GET` | `/api/auth/me` | Return current user profile and roles | Authenticated | Requires valid JWT. |

## Cases

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `GET` | `/api/cases` | Paged case queue | Analyst, Manager, Admin | Analyst scope is server-limited to assigned cases; supports search, filters, sort, pagination, and query-time overdue. |
| `GET` | `/api/cases/{caseId}` | Case detail | Analyst assigned to case, Manager, Admin | Object-level access enforced by API service; returns latest `RowVersion`. |
| `POST` | `/api/cases` | Create case | Manager, Admin | Calculates `DueAtUtc` from active `SlaRule`, starts `New` and unassigned, writes `CaseCreated`. |

## Notes

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `GET` | `/api/cases/{caseId}/notes` | List notes | Analyst assigned to case, Manager, Admin | Same object-level access as case detail. |
| `POST` | `/api/cases/{caseId}/notes` | Add note | Analyst assigned to case, Manager, Admin | Trims and validates body; writes `NoteAdded`. |

## Timeline

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `GET` | `/api/cases/{caseId}/timeline` | Business audit timeline | Analyst assigned to case, Manager, Admin | Same object-level access as case detail; exposes supported case audit events. |

## Assignment

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `PATCH` | `/api/cases/{caseId}/assign` | Assign or reassign case | Manager, Admin | Target must be active Analyst; closed cases and same-assignee requests are rejected; supplied stale `RowVersion` returns conflict. |

## Status

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `PATCH` | `/api/cases/{caseId}/status` | Workflow status transition | Analyst assigned to case, Manager, Admin | Requires `RowVersion`; blocks direct `PendingApproval`; blocks High/Critical normal close; writes status history and audit. |

## Closure Approval

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `POST` | `/api/cases/{caseId}/closure-request` | Request High/Critical closure approval | Analyst assigned to case, Manager, Admin | Requires `Resolved` High/Critical case, required `RowVersion`, no duplicate pending approval; moves case to `PendingApproval`. |
| `GET` | `/api/approvals/pending` | Pending approval queue | Manager, Admin | Manager/Admin-only queue with paging and latest case `RowVersion`. |
| `POST` | `/api/approvals/{approvalId}/approve` | Approve closure | Manager, Admin | Pending request and related `PendingApproval` case required; closes case and writes audit/history. |
| `POST` | `/api/approvals/{approvalId}/reject` | Reject closure | Manager, Admin | Pending request and related `PendingApproval` case required; returns case to `InReview` and writes audit/history. |

## Dashboard

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `GET` | `/api/dashboard/summary` | Operational metric summary | Analyst, Manager, Admin | SQL/EF-backed; Analyst metrics are scoped to assigned cases and assigned-case pending approvals. |
| `GET` | `/api/dashboard/breakdowns` | Breakdowns by status, priority, type, assignee | Analyst, Manager, Admin | SQL/EF-backed route query metadata drives queue drill-down links. |

## Lookups

| Method | Route | Purpose | Roles | Key backend rule |
| --- | --- | --- | --- | --- |
| `GET` | `/api/case-types` | Active case type lookup | Authenticated | Read-only active case types. |
| `GET` | `/api/users/analysts` | Active Analyst lookup | Manager, Admin | Manager/Admin-only assignment target list. |
| `GET` | `/health` | API health check | Anonymous | Returns API health payload. |

## Not Exposed

OpsFlow does not expose user administration, role administration, case type mutation, note edit/delete, export/report builders, notifications, worker controls, real-time messaging, or technical audit endpoints.
