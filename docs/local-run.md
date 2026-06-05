# Local Run

Run commands from the repository root unless a step says otherwise. Docker Compose provides the SQL Server local dependency. The API and Angular app run from source for local development and demo review.

## Prerequisites

- .NET SDK 10.0.x
- Node.js 22.x
- npm 10.x
- Docker with Linux container support
- Optional Angular CLI; `npm start` uses the project-local tooling

## Fast Demo Run

1. Start SQL Server 2022:

   ```bash
   docker compose up -d
   ```

2. Run the API:

   ```bash
   dotnet run --project src/OpsFlow.Api/OpsFlow.Api.csproj --urls http://localhost:5080
   ```

   In Development, the API applies EF Core migrations and seeds deterministic local data on startup.

3. Confirm health:

   ```bash
   curl http://localhost:5080/health
   ```

   Expected response:

   ```json
   { "status": "ok", "service": "OpsFlow.Api" }
   ```

4. Run Angular:

   ```bash
   cd src/OpsFlow.Web
   npm ci
   npm start
   ```

5. Open:

   ```text
   http://localhost:4200
   ```

The Angular dev server proxies `/api` and `/health` to `http://localhost:5080`.

## Demo Accounts

All seeded accounts use password `Password123!`.

| Role | Email |
| --- | --- |
| Admin | `admin@opsflow.local` |
| Manager | `manager@opsflow.local` |
| Analyst | `analyst1@opsflow.local` |
| Analyst | `analyst2@opsflow.local` |
| Analyst | `analyst3@opsflow.local` |

## SQL Server

Docker Compose runs SQL Server 2022 on `localhost:1433` using container name `opsflow-sql`.

Default local password:

```text
OpsFlow_dev_2026!
```

Override for the container:

```bash
OPSFLOW_SQL_PASSWORD='Your_Strong_Password_123!' docker compose up -d
```

PowerShell:

```powershell
$env:OPSFLOW_SQL_PASSWORD = "Your_Strong_Password_123!"
docker compose up -d
```

If the SQL password changes after the volume already exists, reset the volume or update the API connection string to match.

## Reset And Reseed

Removing the SQL volume resets local database state. The next API startup reapplies migrations and seed data.

```bash
docker compose down -v
docker compose up -d
dotnet run --project src/OpsFlow.Api/OpsFlow.Api.csproj --urls http://localhost:5080
```

## Full Validation

Backend:

```bash
dotnet restore OpsFlow.sln
dotnet build OpsFlow.sln --no-restore
dotnet test OpsFlow.sln --no-build
```

Frontend:

```bash
cd src/OpsFlow.Web
npm ci
npm run build
npm test -- --watch=false
```

## Troubleshooting

| Symptom | Likely cause | Fix |
| --- | --- | --- |
| SQL Server will not start | Port `1433` is already in use | Stop the conflicting service or change the compose port mapping. |
| API cannot connect to SQL | SQL container is still starting | Wait for the compose health check, then restart the API. |
| API port is unavailable | Port `5080` is already in use | Stop the other process or run API with another URL and update `proxy.conf.json`. |
| Angular API calls fail | API is not running or proxy target does not match | Confirm `http://localhost:5080/health` and `src/OpsFlow.Web/proxy.conf.json`. |
| Login fails after database changes | Seed data was removed or API did not reseed | Reset the SQL volume and start the API in Development. |
| SQL password override does not work | Existing SQL volume still has the old password | Run `docker compose down -v`, then start with the desired password. |
| Frontend install/build fails | Node/npm version mismatch or stale dependencies | Use Node.js 22.x, npm 10.x, then run `npm ci`. |
| Health check fails immediately after compose up | SQL startup delay | Wait for SQL Server readiness; the container health check can take several attempts. |

Next: run [Manual Smoke Checklist](manual-smoke-checklist.md).
