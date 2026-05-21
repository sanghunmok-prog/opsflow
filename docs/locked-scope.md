# OpsFlow Locked Scope

## Project Type

OpsFlow is a 4-week portfolio-ready internal business application.

It is an enterprise-style case and exception management system built to demonstrate practical full-stack .NET/Angular/SQL skills.

It is not a SaaS platform, microservices platform, AI product, healthcare claims product, EDI system, or payment system.

## Must Have

These features are required for the final portfolio version.

### 1. Single full-stack app

Include:

- ASP.NET Core Web API
- Angular frontend
- SQL Server
- EF Core
- ASP.NET Core Identity/JWT

Reason:

This directly matches the target .NET/Angular/SQL full-stack developer profile.

Do not include:

- microservices
- Kubernetes
- worker services
- distributed architecture

### 2. Analyst / Manager / Admin roles

Include:

- exactly three roles
- role-based authorization
- API-enforced authorization

Reason:

This makes OpsFlow look like a real internal business application, not a toy CRUD app.

Do not include:

- Supervisor
- Auditor
- TeamLead
- extra custom roles

### 3. Object-level authorization

Include:

- Analyst can view/update assigned cases only
- Manager/Admin can view/update all cases

Reason:

This is a strong practical backend signal.

Do not include:

- frontend-only authorization
- button hiding as the only security control

### 4. Server-side case queue

Include:

- pagination
- filtering
- sorting
- search
- role-aware query behavior

Reason:

This shows real application developer skill.

Do not include:

- client-side filtering over all rows
- fake/mock case data for final UI

### 5. Case detail workflow

Include:

- case metadata
- SLA data
- notes
- assignee
- status
- audit timeline
- approval state when relevant

Reason:

This turns the project from a ticket list into a workflow application.

Do not include:

- note edit/delete unless explicitly added later
- overly complex state machine library

### 6. SLA rule and overdue calculation

Include:

- CaseType + Priority based SLA
- DueAt calculation
- query-time overdue calculation

Reason:

This is a core anti-CRUD business rule.

Do not include:

- business-hour calendar
- holiday logic
- timezone-specific SLA
- SLA escalation worker

### 7. Manager approval workflow

Include:

- High/Critical closure request
- PendingApproval case status
- ApprovalRequest
- approve/reject behavior later in PR-10

Reason:

This is the main business workflow signal.

Do not include:

- notification system
- email
- outbox
- background dispatch

### 8. Business audit timeline

Include:

- CaseCreated
- NoteAdded
- Assigned
- StatusChanged
- ClosureRequested
- ApprovalApproved
- ApprovalRejected
- CaseReopened

Reason:

This shows traceability and internal application maturity.

Do not include:

- TechnicalAuditLog
- SaveChangesInterceptor technical audit
- read/view/login audit

### 9. SQL-backed dashboard

Include:

- metrics calculated from SQL data
- role-aware dashboard behavior
- drill-down links later in UI

Reason:

This demonstrates SQL/query/application reporting ability.

Do not include:

- DashboardMetrics table
- fake dashboard data
- advanced reporting/export

### 10. Tests, CI, README, and demo readiness

Include:

- meaningful backend tests
- frontend build
- CI
- README
- diagrams
- screenshots
- local/Docker run instructions
- demo script

Reason:

This is required for a portfolio-ready project.

Do not include:

- claims of features not implemented
- screenshots/demo claims before they exist

## Should Have

These are valuable but should not block core progress.

### RowVersion optimistic concurrency

Use for key mutations such as assignment, status transition, approval.

Do not apply it everywhere if it delays core implementation.

### ProblemDetails error responses

Use for consistent API errors.

Important cases:

- 401 unauthenticated
- 403 forbidden
- 404 not found
- 409 concurrency or duplicate pending approval
- 422 invalid business transition

### Docker Compose local demo

Useful for portfolio review.

Azure deployment is optional and lower priority than local reproducibility.

### Angular loading/error/empty states

Required for polish, but keep simple.

## Optional

Only add these if core scope is complete and time remains.

- Azure deployment
- minimal SLA rule admin read-only/config screen
- Swagger/OpenAPI polish
- simple chart polish
- frontend unit tests

## Explicitly Cut

Do not build these in this project.

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
- email notification
- real-time updates
- SignalR
- advanced reporting/report builder
- stored DashboardMetrics table
- persisted IsOverdue column

## Data Rules

Use deterministic synthetic seed data.

Seed data must be industry-neutral.

Allowed examples:

- Customer Request
- Billing Question
- Access Issue
- Data Correction
- Service Exception
- Internal Review
- Compliance Review

Do not use:

- real company data
- real customer data
- PHI
- healthcare claims data
- EDI data
- payment card data
- Optum/client-specific data
