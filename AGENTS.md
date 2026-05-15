# OpsFlow Agent Rules

## Project Identity

OpsFlow is an industry-neutral enterprise case and exception management system for internal operations teams. It is not a ticket app, helpdesk clone, or Jira clone.

## Scope Control

- Implement one PR scope at a time.
- Do not implement future PR features early.
- Keep public docs in English and do not copy private planning material into tracked files.
- Do not commit changes unless the user explicitly asks.

## PR-00 Boundaries

PR-00 is setup only:

- Repository structure
- ASP.NET Core API skeleton
- Angular app skeleton
- xUnit test project
- Docker Compose SQL Server placeholder
- README/docs placeholders
- Basic GitHub Actions CI

Do not implement entities, EF migrations, authentication, case APIs, SLA logic, approval workflow, audit logging, dashboard metrics, or business UI features in PR-00.

## Portfolio Differentiators For Later PRs

- SLA tracking and overdue calculation
- Role-based authorization
- Manager approval workflow
- Audit logging
- SQL-backed dashboard metrics

## Coding Standards

- Keep controllers thin; put business rules in application services.
- Use DTOs for API request/response; do not expose EF entities directly.
- Enforce authorization in ASP.NET Core APIs; Angular route guards are UX only.
- Use ProblemDetails for validation, authorization, workflow, and concurrency errors.
- Add tests for business rules in the same PR that introduces them.
- Keep public-facing documentation in English.
