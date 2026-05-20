# Architecture

OpsFlow uses a single Angular frontend, a single ASP.NET Core Web API, and SQL Server. The architecture is intentionally straightforward so the portfolio signal comes from complete enterprise workflow implementation rather than unnecessary distributed-system complexity.

## Layers

- `src/OpsFlow.Api`: HTTP API, request/response boundaries, health endpoint, and future API configuration.
- `src/OpsFlow.Web`: Angular client application.
- `tests/OpsFlow.Api.Tests`: backend test project.
- `src/OpsFlow.Api/Data`: EF Core `OpsFlowDbContext`, domain entities, enums, migrations, and deterministic seed data.

Future PRs may introduce additional backend projects if needed for domain, application, and infrastructure separation. PR-01 keeps the backend in the API project so the database foundation remains simple and reviewable.

## Data Layer

OpsFlow uses EF Core with SQL Server for the relational workflow model. The schema includes users, case types, SLA rules, cases, notes, status histories, assignment histories, approval requests, and audit logs.

Enums are stored as strings for SQL readability. Business timestamps use UTC fields with a `Utc` suffix. `Cases.RowVersion` is configured as a SQL Server `rowversion` concurrency token for later command endpoints.

Development seeding is deterministic and generated at runtime, not through large `HasData` migration blocks.

## Architecture Decision

OpsFlow uses a single API and SQL Server rather than microservices because the portfolio signal is workflow completeness, transaction boundaries, authorization, auditability, and reporting. A single deployable API keeps the implementation realistic for an internal operations system and avoids distributed-system scope that would not improve the core demo.

## Future Design Notes

- Controllers should stay thin.
- Business rules should live outside controllers.
- API DTOs should stay separate from persistence entities.
- Authorization must be enforced server-side.
- Dashboard data must come from SQL-backed aggregation, not static frontend values.

## ERD

See [data-model.md](data-model.md).
