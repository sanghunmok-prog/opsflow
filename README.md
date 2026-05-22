# OpsFlow

OpsFlow is an industry-neutral enterprise case and exception management system for internal operations teams.

## Portfolio Goal

OpsFlow is a 4-week portfolio project for .NET / C# / Angular / SQL Server full-stack developer roles. The goal is to demonstrate production-style delivery of an internal business workflow application: clean PR history, server-side workflow rules, authorization, relational data modeling, CI, documentation, and reproducible local setup.

PR-00 established the repository skeleton, application projects, local development wiring, and documentation placeholders. PR-01 added the SQL Server / EF Core database foundation and deterministic synthetic seed data. PR-01A aligns that foundation with ASP.NET Core Identity-backed users/roles and the locked OpsFlow workflow direction. PR-02 adds backend demo login, JWT issuing, `/api/auth/me`, and role authorization policies. PR-03 adds authenticated, role-aware backend case queue and case detail read APIs. Case mutation workflow services, dashboard endpoints, and Angular business screens are intentionally deferred to later PRs.

## Tech Stack

- Backend: ASP.NET Core Web API on .NET 10
- Frontend: Angular 21 and TypeScript
- Database: SQL Server 2022 for local development
- Data access: Entity Framework Core with ASP.NET Core Identity persistence
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

These features are planned portfolio differentiators. The current foundation includes schema, deterministic seed data, backend authentication, and role-aware case read APIs that support them, but it does not implement case mutation APIs, SLA services, approval workflow behavior, audit services, dashboard endpoints, or Angular business UI.

## Local Setup

Prerequisites:

- .NET SDK 10.0.x
- Node.js 22.x and npm 10.x
- Angular CLI 21.x
- Docker Desktop with WSL integration

Initial verification commands:

```bash
dotnet restore OpsFlow.sln
dotnet build OpsFlow.sln
dotnet test OpsFlow.sln
cd src/OpsFlow.Web
npm install
npm run build
cd ../..
docker compose config
```

## Local Database Setup

Start SQL Server:

```bash
docker compose up -d
```

Apply EF Core migrations:

```bash
dotnet ef database update \
  --project src/OpsFlow.Infrastructure/OpsFlow.Infrastructure.csproj \
  --startup-project src/OpsFlow.Api/OpsFlow.Api.csproj
```

Run the API in development mode to apply any pending migrations and seed local demo data:

```bash
dotnet run --project src/OpsFlow.Api/OpsFlow.Api.csproj
```

The development connection string targets `localhost,1433` and database `OpsFlowDb`. Override it with standard ASP.NET Core configuration, for example:

```bash
ConnectionStrings__OpsFlowDb="Server=localhost,1433;Database=OpsFlowDb;User Id=sa;Password=<local-password>;TrustServerCertificate=True;Encrypt=True;" \
dotnet run --project src/OpsFlow.Api/OpsFlow.Api.csproj
```

## Seed Data

The development seeder is deterministic and safe to run repeatedly against an already-seeded database. It creates:

- 5 demo users across Admin, Manager, and Analyst roles
- exactly 3 Identity roles: Analyst, Manager, Admin
- 6 case types
- 24 SLA rules covering every case type and priority combination
- 320 synthetic operations cases
- Sample notes, status histories, assignment histories, approval requests, and audit logs

PR-02 adds backend login and JWT issuing for these seeded demo users.

## Demo Auth

The backend exposes `POST /api/auth/login` for seeded demo users and `GET /api/auth/me` for the authenticated profile. Login returns a JWT bearer access token containing user identity, email, display name, and role claims.

Demo password for all accounts: `Password123!`

| Role | Email | Purpose |
| --- | --- | --- |
| Admin | admin@opsflow.local | Full demo access |
| Manager | manager@opsflow.local | Approval and reassignment workflow |
| Analyst | analyst1@opsflow.local | Assigned case workflow |
| Analyst | analyst2@opsflow.local | Assigned case workflow |
| Analyst | analyst3@opsflow.local | Assigned case workflow |

## Case Read API

Authenticated users can read cases through:

- `GET /api/cases`
- `GET /api/cases/{id}`

`GET /api/cases` supports `page`, `pageSize`, `search`, `status`, `priority`, `caseTypeId`, `assignedToUserId`, `sortBy`, and `sortDirection`. Analysts are constrained server-side to their own assigned cases. Managers and Admins can read all cases and filter by assignee.

These endpoints return DTOs only. They do not expose notes, assignment history, status history, approval actions, dashboard metrics, Identity internals, or case mutation behavior.

## Screenshots

Screenshots will be added as the Angular workflow screens are implemented.

## Architecture And Data Model

- Architecture notes: [docs/architecture.md](docs/architecture.md)
- API contract notes: [docs/api-contract.md](docs/api-contract.md)
- PR plan: [docs/pr-plan.md](docs/pr-plan.md)
- Demo script: [docs/demo-script.md](docs/demo-script.md)
- Data model: [docs/data-model.md](docs/data-model.md)

The database schema was introduced in PR-01 and corrected in PR-01A. Case read API contracts are available now; mutation workflows and UI screens will be implemented in later PRs.
