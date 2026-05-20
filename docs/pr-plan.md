# PR Plan

OpsFlow is delivered as a sequence of small reviewable PRs. Each PR should have one primary concern and should avoid implementing future scope early.

| PR | Title | Status | Primary Signal |
| --- | --- | --- | --- |
| PR-00 | chore: initialize solution, local dev, README skeleton, basic CI | Complete | Professional repo setup |
| PR-01 | feat(db): add core schema, EF migrations, and synthetic seed data | Current | SQL-backed foundation |
| PR-02 | feat(auth): add demo login, role claims, and API authorization policies | Planned | RBAC/auth |
| PR-03 | feat(cases): add paginated role-aware case queue and detail API | Planned | Enterprise queue backend |
| PR-04 | feat(sla): calculate due dates and overdue status from SLA rules | Planned | Operations business rule |
| PR-05 | feat(ui): add Angular shell, login flow, and role-aware navigation | Planned | Full-stack integration |
| PR-06 | feat(ui): implement case queue with filters, pagination, SLA badges | Planned | Recruiter-visible UI |
| PR-07 | feat(cases): add case detail view and notes | Planned | Forms/API/DB integration |
| PR-08 | feat(assignments): add manager-only reassignment workflow | Planned | Role-separated workflow |
| PR-09 | feat(workflow): enforce status transitions with history and audit | Planned | Not CRUD |
| PR-10 | feat(approvals): add manager approval workflow for high-priority closure | Planned | Business rule enforcement |
| PR-11 | feat(dashboard): add SQL-backed metrics and drill-downs | Planned | Reporting/SQL aggregation |
| PR-12 | test: harden validation, concurrency, authorization, and CI coverage | Planned | Maintainability |
| PR-13 | docs: add portfolio README, diagrams, screenshots, demo video, deployment guide | Planned | Portfolio readiness |

## Current PR

PR-01 adds EF Core SQL Server support, core schema, the initial migration, deterministic local seed data, and model/seed tests.

PR-01 intentionally does not implement authentication, case APIs, SLA calculation services, approval workflow behavior, audit services, dashboard endpoints, or Angular business UI.
