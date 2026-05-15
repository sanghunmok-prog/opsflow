# OpsFlow

OpsFlow is an industry-neutral enterprise case and exception management system for internal operations teams.

## Portfolio Goal

OpsFlow is a 4-week portfolio project for .NET / C# / Angular / SQL Server full-stack developer roles. The goal is to demonstrate production-style delivery of an internal business workflow application: clean PR history, server-side workflow rules, authorization, relational data modeling, CI, documentation, and reproducible local setup.

PR-00 only establishes the repository skeleton, application projects, local development wiring, and documentation placeholders. Business features are intentionally deferred to later PRs.

## Tech Stack

- Backend: ASP.NET Core Web API on .NET 10
- Frontend: Angular 21 and TypeScript
- Database: SQL Server 2022 for local development
- Data access: Entity Framework Core
- Tests: xUnit
- Local orchestration: Docker Compose
- CI: GitHub Actions

## Key Features

Planned portfolio differentiators:

- SLA tracking and overdue calculation
- Role-based authorization with server-side enforcement
- Manager approval workflow
- Audit logging
- SQL-backed dashboard metrics

These features are not implemented in PR-00.

## Local Setup

Prerequisites:

- .NET SDK 10.0.x
- Node.js 22.x and npm 10.x
- Angular CLI 21.x
- Docker Desktop with WSL integration

Initial verification commands:

```bash
dotnet build
dotnet test
cd src/OpsFlow.Web
npm install
npm run build
cd ../..
docker compose config
```

Run commands will be expanded as the API, database, and UI integration are implemented.

## Demo Accounts

Demo accounts will be added with the authentication and authorization PR.

| Role | Email | Purpose |
| --- | --- | --- |
| Analyst | TBD | Assigned case workflow |
| Manager | TBD | Approval and reassignment workflow |
| Admin | TBD | Full demo access |

## Screenshots

Screenshots will be added as the Angular workflow screens are implemented.

## Architecture And ERD

- Architecture notes: [docs/architecture.md](docs/architecture.md)
- API contract notes: [docs/api-contract.md](docs/api-contract.md)
- PR plan: [docs/pr-plan.md](docs/pr-plan.md)
- Demo script: [docs/demo-script.md](docs/demo-script.md)

The ERD will be added after the database schema PR.
