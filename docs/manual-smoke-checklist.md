# Manual Smoke Checklist

Start from a clean seeded local database. Use the reset/reseed steps in [Local Run](local-run.md) when needed. Complete automated build/test before manual validation when the environment supports it.

## Seeded Demo Scenario Map

Seeded case numbers are deterministic for a given seed year and use `OPF-{year}-{sequence}`. Scenario selection can use filters instead of fixed numbers.

| Scenario | Role/account | Starting case state | Purpose | Expected result |
| --- | --- | --- | --- | --- |
| Manager dashboard review | `manager@opsflow.local` | Any seeded database | Validate global SQL-backed metrics | Counts and breakdowns load and drill into queue filters. |
| Analyst assigned workflow | `analyst1@opsflow.local` | Assigned/InReview/WaitingInfo case assigned to analyst | Validate object scope and status workflow | Analyst can open and update assigned case only. |
| High/Critical approval workflow | Analyst then Manager | High/Critical `Resolved` case | Validate closure request and approval gate | Request moves case to `PendingApproval`; approval closes it. |
| Low/Medium normal close | Manager | Low/Medium `Resolved` case | Validate non-approval closure | Manager closes through normal status endpoint. |
| Object access negative check | Analyst | Case assigned to another analyst or unassigned | Validate server-side object authorization | Detail, notes, and timeline access are denied. |
| RowVersion two-tab conflict | Manager or assigned Analyst | Mutable non-closed case | Validate stale write conflict | Second stale mutation returns `409`. |
| Duplicate approval conflict | Analyst or Manager | High/Critical `PendingApproval` case | Validate one pending approval per case | Second closure request returns `409`. |
| Timeline audit verification | Manager | Case used for mutation path | Validate business audit events | Timeline shows expected event sequence and actor display. |

## Automated Validation

Backend:

```bash
dotnet restore OpsFlow.sln
dotnet build OpsFlow.sln --no-restore
dotnet test OpsFlow.sln --no-build
```

Frontend:

```bash
cd src/OpsFlow.Web
npm ci
npm run build
npm test -- --watch=false
```

## API Health

1. Start SQL Server with `docker compose up -d`.
2. Start API with `dotnet run --project src/OpsFlow.Api/OpsFlow.Api.csproj --urls http://localhost:5080`.
3. Call `GET http://localhost:5080/health`.
4. Expect status `200` and service payload.

## Login

- Manager login succeeds.
- Admin login succeeds.
- Analyst login succeeds.
- Invalid credentials return `401`.

## Case Creation And SLA

- Manager/Admin can create a case.
- Analyst cannot create a case.
- New case starts `New` and unassigned.
- `DueAtUtc` is calculated from the active `SlaRule` for case type and priority.
- `CaseCreated` timeline event appears.
- Missing or invalid required fields show validation errors.

## Dashboard Metrics

- Manager global metrics load.
- Analyst scoped metrics load.
- Open case count matches `Status != Closed` queue filter in the same role scope.
- Overdue count matches `overdue=true` queue filter.
- Pending approvals count matches approval queue for Manager/Admin and assigned-case pending approvals for Analyst dashboard.
- Approval decision decreases pending approval count.
- Closing a case moves it out of open queue metrics.
- Drill-down route queries preserve filters.

## Object-Level Authorization

- Analyst sees only assigned cases in queue.
- Analyst cannot open another Analyst's or unassigned case detail; expect `403`.
- Analyst cannot fetch notes for inaccessible case; expect `403`.
- Analyst cannot fetch timeline for inaccessible case; expect `403`.
- Analyst cannot access approval queue; expect `403`.
- Analyst cannot use Manager/Admin assignment controls.
- Missing token on protected endpoints returns `401`.

## Case Queue

- Search matches case number, title, or description.
- Status filter works.
- Priority filter works.
- Case type filter works.
- Manager/Admin assignee filter works.
- Analyst supplying another `assignedToUserId` is denied.
- Overdue filter uses `Status != Closed && nowUtc > DueAtUtc`.
- Sorting works for case number, created date, due date, priority, and status.
- Page and page size work.
- Route query state is preserved when navigating through dashboard drill-down links.

## Case Detail

- Metadata renders: number, title, type, priority, status, assignee, creator.
- SLA due date and overdue state render.
- Lifecycle timestamps render where exposed by the UI/API.
- Latest `rowVersion` is present in API response.
- Notes list loads.
- Timeline loads.
- Allowed action controls match role, assignment, and workflow state.

## Notes

- Valid note is accepted and trimmed.
- Empty note is rejected with `400`.
- Note appears in the note list.
- `NoteAdded` timeline event appears.

## Assignment

- Manager/Admin can assign/reassign an active Analyst with reason.
- Same-assignee request is rejected with `400`.
- Invalid, inactive, or non-Analyst assignee is rejected with `400`.
- Closed case assignment is rejected with `400`.
- Stale supplied `RowVersion` returns `409`.
- Assignment history and `Assigned` timeline event are written.
- `New -> Assigned` status side effect occurs when assigning a `New` case.

## Status Transition

- Assigned case can move to `InReview` or `WaitingInfo`.
- `InReview` can move to `WaitingInfo` or `Resolved`.
- `WaitingInfo` can move to `InReview` or `Resolved`.
- Low/Medium `Resolved -> Closed` succeeds for Manager/Admin.
- High/Critical normal close is blocked with workflow error.
- Direct `PendingApproval` status update is blocked.
- Manager/Admin can reopen `Closed` case to `Reopened`.
- Analyst transitions are limited to assigned cases and the Analyst transition set.
- Invalid transition returns workflow error.
- Stale required `RowVersion` returns `409`.

## Approval Request

- High/Critical `Resolved` case can request closure approval.
- Assigned Analyst can request for own assigned case.
- Manager/Admin can request globally.
- Low/Medium request is rejected.
- Non-`Resolved` request is rejected.
- Duplicate pending approval returns `409`.
- Case status becomes `PendingApproval`.
- Approval record is created.
- `ClosureRequested` timeline event is written.

## Approval Decisions

- Pending queue loads for Manager/Admin.
- Approve closes case and sets `ClosedAtUtc`.
- Reject returns case to `InReview`.
- Already-decided approval returns `409`.
- Stale supplied `RowVersion` returns `409`.
- Related case state conflict returns `409` if the case no longer is `PendingApproval`.
- Timeline event is written for approval or rejection.

## RowVersion Two-Tab Test

1. Open the same case detail in two browser tabs.
2. In tab A, perform assignment or status update.
3. In tab B, without refresh, perform another mutation using the stale row version.
4. Expected: `409` conflict with readable error.
5. Refresh or reload detail and confirm the latest row version.

## Error Handling Matrix

| Scenario | Expected HTTP status | Expected UI behavior |
| --- | --- | --- |
| Invalid credentials | `401` | Login remains blocked with readable message. |
| Missing token | `401` | User is redirected or prompted to authenticate. |
| Analyst object access denied | `403` | Access denied message or navigation away from inaccessible record. |
| Invalid input | `400` | Field/action error is shown. |
| Invalid transition | `422` | Workflow rule message is shown. |
| Duplicate pending approval | `409` | Conflict message is shown. |
| Stale `RowVersion` | `409` | Refresh instruction is shown. |

## Timeline Events

Validate chronological order, actor display, and reason/description where expected:

- `CaseCreated`
- `NoteAdded`
- `Assigned`
- `StatusChanged`
- `ClosureRequested`
- `ApprovalApproved`
- `ApprovalRejected`
- `CaseReopened`

## Final Docs And Media Validation

- README links resolve.
- No unresolved release links exist.
- No unresolved repository tokens exist.
- Diagrams render.
- Local run guide works from clean checkout.
- API docs match implementation.
- GIFs render from `docs/assets/`.
- Full video link points to the public `v1.0-demo` GitHub Release page.
- No `.mp4` file is copied or staged.
- CI status or instructions are current.
