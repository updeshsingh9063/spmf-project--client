# SPFM — Shield Project Financial Management

Internal project financial management application for Shield Security Doors.

> **Alpha status.** The SPFM calculation formulas are **placeholders** pending the
> client's specification. See [Known limitations](#known-limitations).

Design and rationale: **[docs/SYSTEM-DESIGN.md](docs/SYSTEM-DESIGN.md)**

---

## Stack

| | |
|---|---|
| Backend | ASP.NET Core 9 Web API, C# 13 |
| Database | PostgreSQL 16, EF Core 9 (Npgsql) |
| Auth | Microsoft Entra ID (JWT bearer) + MSAL |
| Frontend | Next.js 16, TypeScript, Tailwind v4 |
| Tests | xUnit, Shouldly / Vitest, Playwright |

## Layout

```
backend/
  SPFM.Domain/          entities + calculation engine — zero dependencies
  SPFM.Application/     use cases, DTOs, port interfaces
  SPFM.Infrastructure/  EF Core, repositories, migrations
  SPFM.Api/             controllers, middleware, DI
  SPFM.Tests/           unit, schema and integration tests
frontend/               Next.js app
infrastructure/         docker-compose for local Postgres
docs/                   system design and reference
```

`SPFM.Domain` must never reference EF Core, ASP.NET or HTTP. If the calculation
engine stops being testable without a database, that rule has been broken.

---

## Running locally

### Prerequisites
- .NET 9 SDK
- Node.js 20+
- PostgreSQL 16 (Docker, a local install, or the binaries — see below)

### 1. Database

```bash
docker compose -f infrastructure/docker-compose.yml up -d
```

### 2. Configuration

Local development values are in `backend/SPFM.Api/appsettings.Development.json` —
tenant and client ids only, which are public identifiers, never secrets.

For any other environment, supply configuration as environment variables (double
underscore for nesting). See [.env.example](.env.example):

```
AzureAd__TenantId, AzureAd__ClientId, AzureAd__Audience
ConnectionStrings__DefaultConnection
Cors__AllowedOrigins__0
```

The API **refuses to start** if any of these is missing, naming the ones it needs,
rather than failing later on each request.

### 3. Migrations

```bash
dotnet tool install --global dotnet-ef --version 9.0.0

dotnet ef database update \
  --project backend/SPFM.Infrastructure \
  --startup-project backend/SPFM.Infrastructure
```

To review the SQL before applying it:

```bash
dotnet ef migrations script --idempotent \
  --project backend/SPFM.Infrastructure \
  --startup-project backend/SPFM.Infrastructure
```

Migrations are **never** applied automatically at startup — concurrent instances
would race. Run them as an explicit deployment step.

### 4. Backend

```bash
dotnet run --project backend/SPFM.Api --launch-profile http
```

| | |
|---|---|
| API | http://localhost:5230 |
| Swagger | http://localhost:5230/swagger |
| Liveness | http://localhost:5230/health |
| Readiness | http://localhost:5230/health/ready (includes the database) |

### 5. Frontend

```bash
cd frontend && npm install && npm run dev
```

---

## Tests

```bash
dotnet test                      # backend
cd frontend && npm test          # frontend unit
cd frontend && npm run test:e2e  # Playwright
```

Integration tests need a reachable PostgreSQL server (see
[Running PostgreSQL without Docker](#running-postgresql-without-docker)). Unit and
schema tests need neither a database nor Docker.

---

## If the SDK is not on the default drive

When .NET is installed somewhere other than `C:\Program Files\dotnet` (for example
because the system drive is full), set these before any `dotnet` command:

```powershell
$env:DOTNET_ROOT          = "D:\dotnet"
$env:PATH                 = "D:\dotnet;D:\dotnet-tools;$env:PATH"
$env:NUGET_PACKAGES       = "D:\nuget\packages"
$env:NUGET_HTTP_CACHE_PATH= "D:\nuget\http-cache"
$env:DOTNET_CLI_HOME      = "D:\dotnet-home"
```

`DOTNET_ROOT` matters for tools such as `dotnet-ef`, which otherwise report
"You must install .NET to run this application".

---

## Known limitations

| | |
|---|---|
| **Calculation formulas are placeholders** | `SpfmCalculator` implements standard job-costing arithmetic as a stand-in. Its `EngineVersion` is `placeholder-*` so any stored result computed from invented formulas is identifiable. **Must be replaced before the Alpha is accepted.** |
| **SPFM field list is provisional** | Scalar inputs and line fields are a working shape. Extra fields land in `extra_inputs_json` until the real list is confirmed, then get promoted to typed columns. |
| **No real-project test cases yet** | Requirements §14 and §33 require 2–3 real Shield projects with known results. Not yet supplied. |
| **Project statuses are provisional** | §28 warns against inventing status logic. |
| **QuickBooks is Phase 2** | Mapping columns exist and stay null. No import is built. |
| **No roles or permissions** | Explicitly out of scope (§42). The schema and policy seams allow adding them later. |

## Scope

Included and excluded work is defined by requirements §41–42. QuickBooks import,
user roles, data exports and advanced error monitoring are **not** in this phase.

---

## Running PostgreSQL without Docker

Where Docker is unavailable (or the system drive has no space), the PostgreSQL
binaries run directly from disk — no installer, no service, nothing written outside
the directories below.

```powershell
# One-off: initialise a data directory
D:\pgsql\bin\initdb.exe -D D:\pgdata -U postgres --pwfile=<file with password> -E UTF8 --locale=C

# Start / stop
D:\pgsql\bin\pg_ctl.exe -D D:\pgdata -l D:\pgdata\server.log -o "-p 5432" start
D:\pgsql\bin\pg_ctl.exe -D D:\pgdata stop

# Create the application role and database
D:\pgsql\bin\psql.exe -h localhost -U postgres -c "CREATE ROLE spfm LOGIN PASSWORD 'spfm123';"
D:\pgsql\bin\psql.exe -h localhost -U postgres -c "CREATE DATABASE spfm OWNER spfm;"
```

Binaries: <https://www.enterprisedb.com/download-postgresql-binaries> (16.x, win-x64).

### Integration tests

They create a throwaway database per run and apply the migrations to it, so they
prove the migrations work rather than only that the model is shaped correctly.

```powershell
$env:SPFM_TEST_CONNECTION = "Host=localhost;Port=5432;Database=postgres;Username=postgres;Password=postgres"
dotnet test
```

`SPFM_TEST_SQL=1` prints the generated SQL — the quickest way to see why a write
behaves differently against a real database.

With no PostgreSQL reachable they **fail**, naming the reason, rather than skipping:
a suite that reports green without running is worse than no suite. To exclude them
deliberately:

```powershell
dotnet test --filter "Category!=Integration"
```

### End-to-end tests

```powershell
$env:PLAYWRIGHT_BROWSERS_PATH = "D:\playwright-browsers"   # if not on the system drive
cd frontend
npx playwright install chromium   # first run only
npx playwright test
```
