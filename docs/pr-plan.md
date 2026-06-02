# OpsFlow Final PR Plan

## Rule

Implement one PR at a time.

Do not implement future PR scope early.

The active PR is defined in:

```text
docs/current-pr.md
```

If `docs/current-pr.md` and this file conflict, follow `docs/current-pr.md`.

## PR Sequence

### PR-00 — Initialize solution, local dev, README skeleton, basic CI

Goal:

Create the repository foundation.

Scope:

- solution structure
- backend skeleton
- frontend skeleton
- README skeleton
- basic CI
- health endpoint
- local dev basics

Do not implement:

- entities
- auth
- business APIs
- Angular business screens

### PR-01 — Add core schema, EF migrations, and synthetic seed data

Goal:

Create the database foundation.

Scope:

- core entities
- EF DbContext
- migrations
- seed data
- demo users/roles foundation
- synthetic industry-neutral cases

Do not implement:

- login endpoints
- JWT issuing
- business APIs
- Angular business screens

### PR-01A — Corrective cleanup to align PR-01 foundation

Goal:

Clean up PR-01 so the foundation matches the final locked direction.

Scope:

- Identity-backed users/roles
- correct CaseStatus values
- correct SLA seed values
- AssignmentHistory reason
- remove non-final columns like RequiresApproval
- correct business audit action names
- move code toward Api/Application/Domain/Infrastructure structure
- update docs
- update foundation tests

Do not implement:

- PR-02 auth endpoints
- case APIs
- Angular business UI
- dashboard
- approval actions

### PR-02 — Add demo login, role claims, and API authorization policies

Goal:

Implement authentication and basic API authorization.

Scope:

- demo login
- JWT issuing
- `/api/auth/login`
- `/api/auth/me`
- role claims
- policies for Analyst/Manager/Admin
- basic auth tests

Do not implement:

- object-level case authorization
- case queue
- Angular login UI unless explicitly included by the active PR

### PR-03 — Add paginated role-aware case queue and detail API

Goal:

Implement server-side case query APIs.

Scope:

- `GET /api/cases`
- `GET /api/cases/{id}`
- pagination
- filtering
- sorting
- search
- role-aware query behavior
- DTOs

Do not implement:

- notes creation
- assignment action
- status transition action
- approval endpoints
- dashboard

### PR-04 — Add SLA due date logic and basic case creation API

Goal:

Implement SLA calculation and basic case creation.

Scope:

- SlaService
- DueAt calculation
- IsOverdue DTO/query calculation
- overdue filter
- `POST /api/cases`
- create validation

Do not implement:

- background jobs
- escalation
- notification
- advanced admin configuration

### PR-05 — Add Angular shell, login flow, and role-aware navigation

Goal:

Connect Angular to authentication.

Scope:

- login page
- token storage
- auth interceptor
- route guard
- role-aware navigation
- basic layout

Do not implement:

- full case queue UI
- dashboard UI
- approval UI

### PR-06 — Implement case queue with filters, pagination, SLA badges, and simple create form

Goal:

Build the first real full-stack business screen.

Scope:

- case queue table
- filters
- search
- pagination
- SLA badge
- manager/admin simple create form
- route query params

Do not implement:

- saved filters
- bulk actions
- advanced charts

### PR-07 — Add case detail view, notes, and basic timeline

Goal:

Build case detail workflow.

Scope:

- case detail page
- notes list
- add note
- note validation
- basic audit timeline
- metadata/SLA display

Do not implement:

- note edit/delete
- assignment action
- status action
- approval action

### PR-08 — Add manager-only reassignment workflow

Goal:

Implement assignment workflow.

Scope:

- manager/admin assignment endpoint
- AssignmentHistory
- assignment reason
- assignment audit event
- manager-only UI panel

Do not implement:

- status transitions
- approval workflow
- notification

### PR-09 — Enforce status transitions with history, audit, and concurrency

Goal:

Implement workflow enforcement.

Scope:

- status transition matrix
- StatusHistory
- StatusChanged audit
- RowVersion concurrency
- invalid transition errors

Do not implement:

- approval approve/reject endpoints
- background workers

### PR-10 — Add manager approval workflow for high-priority closure

Goal:

Implement High/Critical closure approval.

Status:

- Implemented in this PR-10 change.

Scope:

- closure request
- PendingApproval status
- pending approvals queue/API
- approve
- reject
- approval audit events
- duplicate pending approval prevention

Do not implement:

- email notification
- outbox
- background dispatch

### PR-11 — Add SQL-backed dashboard metrics and drill-downs

Goal:

Implement dashboard reporting.

Scope:

- open cases
- overdue open cases
- pending approvals
- average open case age
- SLA breach rate
- breakdowns
- role-aware dashboard
- drill-down links

Do not implement:

- advanced reporting
- report builder
- CSV/export
- DashboardMetrics table

### PR-12 — Harden validation, ProblemDetails, concurrency, authorization, and CI coverage

Goal:

Improve quality and reliability.

Scope:

- validation
- ProblemDetails
- authorization tests
- SLA tests
- workflow tests
- approval tests
- dashboard tests
- concurrency tests
- CI polish

Do not implement:

- new product features

### PR-13 — Finalize README, diagrams, screenshots, Docker/local demo, and demo video script

Goal:

Make the project portfolio-ready.

Scope:

- README
- screenshots
- ERD
- architecture diagram
- API map
- demo accounts
- Docker/local run guide
- demo script
- final smoke test

Do not implement:

- new features
- unverified demo claims
- Azure troubleshooting that delays completion

## Active PR Handling

The active PR is not committed as a permanent repository document.

For each implementation step, the active PR is supplied by the current Codex prompt.

`docs/current-pr.md` may be used as a local scratch file during a PR, but it should not be committed.

## Completion Rule

After each PR:

- build must pass if possible
- relevant tests must pass if possible
- docs must not contradict locked scope
- no forbidden features may be added
- next PR must not be started until explicitly requested
