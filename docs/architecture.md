# Architecture

OpsFlow uses a single Angular frontend, a single ASP.NET Core Web API, and SQL Server. The architecture is intentionally straightforward so the portfolio signal comes from complete enterprise workflow implementation rather than unnecessary distributed-system complexity.

## Planned Layers

- `src/OpsFlow.Api`: HTTP API, request/response boundaries, health endpoint, and future API configuration.
- `src/OpsFlow.Web`: Angular client application.
- `tests/OpsFlow.Api.Tests`: backend test project.

Future PRs may introduce additional backend projects if needed for domain, application, and infrastructure separation. PR-00 keeps the skeleton minimal and buildable.

## Future Design Notes

- Controllers should stay thin.
- Business rules should live outside controllers.
- API DTOs should stay separate from persistence entities.
- Authorization must be enforced server-side.
- Dashboard data must come from SQL-backed aggregation, not static frontend values.

## ERD

The ERD will be added after the database schema and EF migration PR.
