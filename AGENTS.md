# AGENTS.md — OpsFlow Coding Agent Rules

## Source of Truth

For each task, the active PR prompt is the implementation authority.

Before making code changes, also read:

- `docs/locked-scope.md`
- `docs/pr-plan.md`

If `docs/current-pr.md` exists, read it as additional context. If it conflicts with the active PR prompt, the active PR prompt wins.

If these documents conflict, follow this precedence:

1. Active PR prompt
2. `docs/current-pr.md`
3. `docs/locked-scope.md`
4. `docs/pr-plan.md`
5. README/docs
6. Existing code

Do not use old planning assumptions.
Do not infer scope from older design documents.
Do not implement future PRs early.

## Project Goal

OpsFlow is a 4-week portfolio-ready internal case and exception management application.

It is intended to demonstrate:

- ASP.NET Core Web API
- Angular
- SQL Server
- EF Core
- ASP.NET Core Identity/JWT
- role-based authorization
- object-level authorization
- SLA tracking
- manager approval workflow
- business audit timeline
- SQL-backed dashboard metrics
- tests, CI, README, diagrams, and demo readiness

OpsFlow is not:

- a SaaS platform
- a microservices platform
- an AI/LLM project
- a healthcare claims system
- an EDI/payment system
- a production enterprise platform

## Architecture Rules

Use a single full-stack application structure:

- ASP.NET Core Web API backend
- Angular frontend
- SQL Server database
- EF Core ORM
- ASP.NET Core Identity/JWT auth

Recommended backend structure:

- `src/OpsFlow.Api`
- `src/OpsFlow.Application`
- `src/OpsFlow.Domain`
- `src/OpsFlow.Infrastructure`
- `tests/OpsFlow.Api.Tests` or equivalent test project

Keep responsibilities separated:

- API: HTTP endpoints, middleware, startup/configuration
- Application: DTOs, services, interfaces, business use cases
- Domain: entities, enums, domain constants
- Infrastructure: EF Core, DbContext, migrations, seed data, Identity persistence

Do not create microservices.
Do not create a separate worker service.
Do not add distributed-system infrastructure.

## Roles

Use exactly three roles:

- Analyst
- Manager
- Admin

Do not add:

- Supervisor
- Auditor
- TeamLead
- BillingUser
- ClaimsUser
- SupportAdmin
- Any other role

## Core Permission Rules

Analyst:

- Can view assigned cases only
- Can update assigned cases only
- Cannot assign or reassign cases
- Cannot approve or reject approvals
- Cannot access global dashboard data

Manager:

- Can view all cases
- Can assign and reassign cases
- Can approve and reject closure requests
- Can view global dashboard metrics

Admin:

- Same as Manager for core workflow
- Optional configuration access only if explicitly included in the active PR

## SLA Rules

SLA is based on:

- CaseType
- Priority
- TargetHours

Use these target hours:

- Low = 120
- Medium = 72
- High = 24
- Critical = 8

DueAt calculation:

```text
DueAt = CreatedAtUtc + TargetHours
```

Overdue calculation:

```text
IsOverdue = Status != Closed && NowUtc > DueAt
```

Rules:

- Use calendar hours only.
- Store timestamps in UTC.
- Do not implement business hours.
- Do not implement holiday calendars.
- Do not implement timezone-specific SLA logic.
- Do not persist `IsOverdue` as a database column.
- Do not create SLA escalation workers.

## Audit Rules

Only implement business audit events:

- CaseCreated
- NoteAdded
- Assigned
- StatusChanged
- ClosureRequested
- ApprovalApproved
- ApprovalRejected
- CaseReopened

Do not audit:

- read/view events
- login events
- technical EF change events
- background job events
- outbox events

Do not implement:

- TechnicalAuditLog
- SaveChangesInterceptor for technical audit
- full technical before/after database auditing

## Dashboard Rules

Dashboard metrics must be calculated from SQL data by query/service.

Do not create a `DashboardMetrics` table.
Do not hardcode dashboard values.
Do not use frontend mock data for final dashboard metrics.

Required dashboard metrics later in the project:

- open cases
- overdue open cases
- pending approvals
- average open case age
- SLA breach rate
- breakdown by status
- breakdown by priority
- breakdown by type
- breakdown by assignee

## Forbidden Features

Do not add:

- AI/LLM
- microservices
- Kubernetes
- Outbox
- OutboxMessage
- OutboxDispatcher
- background workers
- Worker project
- SLA escalation worker
- EscalationEvent
- JobRun
- TechnicalAuditLog
- SaveChangesInterceptor for technical audit
- real healthcare data
- PHI
- EDI parser
- payment system
- CSV import/export
- saved filters
- large admin panel
- advanced notification system
- real-time updates
- SignalR

## PR Discipline

Implement only the active PR scope.

Do not implement future PR features early.
Do not add features because they seem useful.
Do not expand scope beyond the active PR.

For every PR:

- Read `docs/current-pr.md` first.
- Confirm the active PR number and title.
- Keep the change atomic.
- Update only relevant docs.
- Add or update tests only as required by the active PR.
- Run build/tests if possible.
- Report what changed.
- Report what remains out of scope.
- Report whether any forbidden feature was found or avoided.

## Build and Test Expectations

When possible, run:

```bash
dotnet restore
dotnet build
dotnet test
```

For frontend, use the package manager already present in the repo.

Run one of these only if the corresponding lockfile/package setup exists:

```bash
npm ci
npm run build
```

or:

```bash
pnpm install --frozen-lockfile
pnpm build
```

Do not install unnecessary new packages.
Do not add new production dependencies unless the active PR explicitly requires them.
