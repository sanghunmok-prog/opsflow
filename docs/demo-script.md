# Demo Script

This is a 3-5 minute captioned, no-microphone walkthrough focused on SLA risk, server-side authorization, workflow enforcement, High/Critical closure approval, audit timeline, SQL-backed metrics, and concurrency/error handling proof points.

## Pre-Recording Setup

1. Reset/reseed the database with the steps in [Local Run](local-run.md).
2. Start SQL Server, API, and Angular.
3. Confirm `GET http://localhost:5080/health`.
4. Confirm seeded accounts:
   - `manager@opsflow.local`
   - `analyst1@opsflow.local`
   - `admin@opsflow.local`
   - password `Password123!`
5. Identify a seeded High/Critical `Resolved` case for closure approval.
6. Identify an assigned Analyst case for notes/status workflow.
7. Identify a second Analyst or unassigned case for object-access denial.
8. Identify a mutable case for RowVersion conflict or use the same case in two tabs.
9. Set browser zoom/window size for readable text.
10. Decide whether to briefly show DevTools Network for technical proof.

## Canonical Main Path

1. Login as Manager.
2. Open dashboard.
3. Use a dashboard breakdown to drill into the case queue.
4. Open a High/Critical case.
5. Show SLA due date, query-time overdue state, assignee, notes, timeline, and current `RowVersion` if visible through Network.
6. Assign or confirm assignment to an Analyst.
7. Switch to Analyst.
8. Open the assigned case.
9. Add a note and move the case through review to `Resolved`.
10. Request closure approval.
11. Switch to Manager.
12. Open approval queue.
13. Approve the pending request.
14. Open the case detail and show status `Closed`, status history, and audit timeline events.
15. Return to dashboard and show pending approvals decreased and the closed case no longer counted as open.

## Backend Enforcement Scenes

Use short proof scenes when they fit the runtime:

- Analyst cannot access another user's case detail, notes, or timeline.
- High/Critical resolved case cannot close through the normal status update.
- Direct `PendingApproval` status update is blocked.
- Stale `RowVersion` conflict or duplicate approval conflict appears briefly.

For a shorter primary recording, keep the main walkthrough intact and capture these scenes as supporting GIFs or technical clips.

## Caption Guidance

Use captions that state backend guarantees:

- "The API scopes Analyst reads to assigned cases."
- "Overdue state is derived from SQL data at query time."
- "Dashboard breakdowns drill into server-side queue filters."
- "High/Critical closure is blocked unless approval workflow creates `PendingApproval`."
- "Approval writes status history and business audit events."
- "Stale RowVersion writes return conflict instead of overwriting newer state."

Avoid captions that only describe button clicks.

## Dashboard Moment

Make the metric update specific:

- Pending approvals decreases after approval.
- The case moves out of the open queue when closed.
- Drill-down counts match queue filters.

## Final Frame

End on the technical documentation set:

- [Local Run](local-run.md)
- [API Contract](api-contract.md)
- [Architecture](architecture.md)
- [Workflow](workflow.md)
- [Manual Smoke Checklist](manual-smoke-checklist.md)
