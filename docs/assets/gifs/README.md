# GIF Capture Guidance

The README uses GIF assets stored in `docs/assets/`. This file remains in `docs/assets/gifs/` as capture guidance only.

## GIF Inventory

| File | Purpose |
| --- | --- |
| `docs/assets/gifs/01-master-workflow.gif` | End-to-end Manager workflow: dashboard, approval queue, case detail, approval decision, dashboard refresh. |
| `docs/assets/gifs/02-dashboard-drilldown.gif` | SQL-backed dashboard summary to server-side filtered queue drill-down. |
| `docs/assets/gifs/03-case-detail-workflow.gif` | Case detail workflow with SLA, notes, assignment, status transition, `RowVersion`, and timeline. |
| `docs/assets/gifs/04-approval-workflow.gif` | High/Critical closure approval gate. |

## `docs/assets/gifs/01-master-workflow.gif`

- Purpose: prove the end-to-end Manager path.
- Account/role: `manager@opsflow.local`.
- Starting state: seeded database with pending High/Critical approvals.
- Key screens: dashboard, approval queue, case detail, approval action, refreshed dashboard.
- Backend guarantee: Manager/Admin approval is required before High/Critical cases reach `Closed`.
- Expected ending state: approval count decreases and the case is closed.
- Suggested duration: 20-35 seconds.
- Caption guidance: focus on dashboard metrics, approval decision, audit/history, and refresh.

## `docs/assets/gifs/02-dashboard-drilldown.gif`

- Purpose: prove dashboard metrics connect to real queue filters.
- Account/role: Manager or Analyst for scoped contrast.
- Starting state: seeded database.
- Key screens: dashboard breakdowns, route query change, case queue.
- Backend guarantee: SQL/EF-backed metrics and queue filters share role-aware scope.
- Expected ending state: queue displays the filtered records behind the selected metric.
- Suggested duration: 10-18 seconds.
- Caption guidance: mention server-side filters, pagination, sorting, and role scope.

## `docs/assets/gifs/03-case-detail-workflow.gif`

- Purpose: prove case detail is an operational workflow surface.
- Account/role: Manager/Admin for assignment; assigned Analyst for notes/status.
- Starting state: assigned or assignable non-closed case.
- Key screens: case detail, SLA panel, notes, assignment/status controls, timeline.
- Backend guarantee: mutations validate workflow rules, update `RowVersion`, and write business audit events.
- Expected ending state: note/status/assignment changes appear in detail and timeline.
- Suggested duration: 15-25 seconds.
- Caption guidance: mention `DueAtUtc`, query-time overdue, `RowVersion`, and timeline events.

## `docs/assets/gifs/04-approval-workflow.gif`

- Purpose: prove High/Critical approval-gated closure.
- Account/role: assigned Analyst requests; Manager/Admin decides.
- Starting state: High/Critical `Resolved` case.
- Key screens: case detail closure panel, approval queue, decision action, timeline.
- Backend guarantee: normal close is blocked; closure requires `ApprovalRequest` and Manager/Admin decision.
- Expected ending state: approved case is `Closed` or rejected case returns to `InReview`.
- Suggested duration: 15-25 seconds.
- Caption guidance: emphasize `PendingApproval`, approval request, decision, history, and audit event.

## Optional Technical Proof Capture

Capture only when it fits the documentation or release media:

- Stale `RowVersion` conflict from a two-tab mutation.
- Invalid status transition such as direct `PendingApproval`.
- Analyst object-level authorization denial on another user's case.

Keep these short and focused on the API-backed rule being enforced.

## Capture Standards

- Use readable browser zoom.
- Use seeded data only.
- Do not use real operational data.
- Crop to the app window.
- Do not depend on voiceover.
- Use short captions.
- Avoid exposing secrets, JWTs, or connection strings.
- Verify final file size is reasonable for README rendering.

## README Integration

Update README only with assets that exist and render correctly. Do not leave broken links or unresolved release URLs. Do not copy or commit `.mp4` files.
