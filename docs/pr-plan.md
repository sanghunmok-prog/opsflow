# PR Plan

OpsFlow is delivered as a sequence of small reviewable PRs. Each PR should have one primary concern and should avoid implementing future scope early.

| PR | Title | Primary Signal |
| --- | --- | --- |
| PR-00 | chore: initialize solution, local dev, README skeleton, basic CI | Professional repo setup |
| PR-01 | feat(db): add core schema, EF migrations, and synthetic seed data | SQL-backed foundation |
| PR-02 | feat(auth): add demo login, role claims, and API authorization policies | RBAC/auth |
| PR-03 | feat(cases): add paginated role-aware case queue and detail API | Enterprise queue backend |
| PR-04 | feat(sla): calculate due dates and overdue status from SLA rules | Operations business rule |
| PR-05 | feat(ui): add Angular shell, login flow, and role-aware navigation | Full-stack integration |
| PR-06 | feat(ui): implement case queue with filters, pagination, SLA badges | Recruiter-visible UI |
| PR-07 | feat(cases): add case detail view and notes | Forms/API/DB integration |
| PR-08 | feat(assignments): add manager-only reassignment workflow | Role-separated workflow |
| PR-09 | feat(workflow): enforce status transitions with history and audit | Not CRUD |
| PR-10 | feat(approvals): add manager approval workflow for high-priority closure | Business rule enforcement |
| PR-11 | feat(dashboard): add SQL-backed metrics and drill-downs | Reporting/SQL aggregation |
| PR-12 | test: harden validation, concurrency, authorization, and CI coverage | Maintainability |
| PR-13 | docs: add portfolio README, diagrams, screenshots, demo video, deployment guide | Portfolio readiness |

## Current PR

PR-00 is setup only. It intentionally does not implement database schema, authentication, case APIs, SLA logic, approval workflow, audit logging, or dashboard metrics.
