# Personal Assistant

A self-hosted productivity app: a single user account, a dashboard of modules, and a focus on quick capture and clean reporting. Built as a .NET 10 / EF Core backend with an Angular + Bootstrap frontend.

## Modules

| Module | Status | What's inside |
|---|---|---|
| **Task Management** | Built | Daily Tasks, Periodic Tasks, To-Do List — each with reports (HTML / PDF / Excel / CSV) |
| **Finance Management** | Built | Dashboard, Expense Tracker, Budget, Settings (accounts / categories / payment types / tags) |
| Asset Tracker | Coming soon | — |
| Time Tracker | Coming soon | — |
| Health & Workouts | Coming soon | — |
| Goal Tracker | Coming soon | — |
| Notes | Coming soon | — |

## Tech stack

- **Backend** — .NET 10, ASP.NET Core Web API, EF Core 10 with **dual provider** (SQLite + SQL Server) chosen at startup, ASP.NET Identity + JWT, Swagger, Serilog
- **Frontend** — Angular 21 (standalone APIs, signals, control-flow templates), Bootstrap 5 + custom SCSS theme tokens (light / dark), neon utility classes
- **Reports** — QuestPDF (PDF), ClosedXML (Excel), CsvHelper (CSV), abstracted behind a single `IReportExportService`
- **Persistence** — Soft delete on every domain entity via a SaveChanges interceptor; per-user data isolation via `OwnerUserId` filter

## Repository layout

```
.
├── backend/
│   ├── PersonalAssistant.sln
│   ├── src/
│   │   ├── PersonalAssistant.Api/            # Web host, controllers, Program.cs, Swagger
│   │   ├── PersonalAssistant.Domain/         # Entities, enums (no external deps beyond Identity stores)
│   │   ├── PersonalAssistant.Application/    # DTOs, service interfaces, report contracts
│   │   └── PersonalAssistant.Infrastructure/ # DbContext, EF configurations, service impls, JWT, exporters
│   ├── migrations/
│   │   ├── PersonalAssistant.Migrations.Sqlite/
│   │   └── PersonalAssistant.Migrations.SqlServer/
│   └── tests/PersonalAssistant.Tests/
└── frontend/
    └── personal-assistant-ui/                # Angular workspace
```

## Prerequisites

- **.NET SDK 10** (`dotnet --version` ≥ `10.0.x`)
- **Node 20+** and **npm** (Node 25 works but is not officially supported by Angular)
- **Angular CLI 21** (`npm i -g @angular/cli`)
- *(Optional, only if you switch to SQL Server)* SQL Server LocalDB or any reachable SQL Server instance

## First-time setup

```powershell
# From the repository root
cd backend
dotnet restore
dotnet build

cd ..\frontend\personal-assistant-ui
npm install
```

The backend auto-applies migrations on startup, so there is no manual `database update` step in normal use.

## Running locally

Open two terminals.

**Terminal 1 — Backend** (defaults to SQLite at `./personal-assistant.db`):
```powershell
cd backend\src\PersonalAssistant.Api
dotnet run --launch-profile http
```
- API: <http://localhost:5024>
- Swagger: <http://localhost:5024/swagger>

**Terminal 2 — Frontend**:
```powershell
cd frontend\personal-assistant-ui
ng serve --port 4200 --host 127.0.0.1
```
- UI: <http://127.0.0.1:4200>

Open the UI, register an account, and you're in.

## Switching between SQLite and SQL Server

Edit `backend/src/PersonalAssistant.Api/appsettings.json`:

```jsonc
"Database": {
  "Provider": "Sqlite",   // "Sqlite" or "SqlServer"
  "ConnectionStrings": {
    "Sqlite":    "Data Source=personal-assistant.db",
    "SqlServer": "Server=(localdb)\\MSSQLLocalDB;Database=PersonalAssistant;Trusted_Connection=True;Encrypt=False;"
  }
}
```

Each provider has its own migrations project, so the schema stays in lockstep on both:

```powershell
# After adding a new entity, generate migrations for both providers.
cd backend
dotnet ef migrations add MyChange `
  --project migrations/PersonalAssistant.Migrations.Sqlite `
  --startup-project migrations/PersonalAssistant.Migrations.Sqlite `
  --context AppDbContext

dotnet ef migrations add MyChange `
  --project migrations/PersonalAssistant.Migrations.SqlServer `
  --startup-project migrations/PersonalAssistant.Migrations.SqlServer `
  --context AppDbContext
```

## Authentication

- Open registration is enabled (`POST /api/auth/register`).
- Login (`POST /api/auth/login`) returns a JWT; the frontend stores it in `localStorage` and sends it via an HTTP interceptor.
- `GET /api/auth/me` returns the current user.
- All non-auth endpoints require `[Authorize]` and are scoped to the JWT subject (`OwnerUserId`).

In Swagger, click **Authorize** and paste the JWT (without the `Bearer ` prefix) to exercise authenticated endpoints.

## Theme & look

- Light/dark mode toggle is in the header. Preference is persisted to `localStorage` and respects the OS preference on first load.
- Theming is driven by CSS custom properties in [`src/styles/_tokens.scss`](frontend/personal-assistant-ui/src/styles/_tokens.scss).
- Neon borders are utility classes (`.neon`, `.neon-cyan`) layered on top of Bootstrap.

## Module quick tour

### Task Management (`/tasks`)

Three tabs share the same shell. Each has a **Reports** button (CSV / Excel / PDF / on-screen).

- **Daily Tasks** — Group recurring daily checklists. Per-day completion + note. Top counts strip + per-group counts. Reports: day-wise grid and task-wise summary.
- **Periodic Tasks** — Tasks that repeat every N days/weeks/months/years. Track last-done, computed next-due (color-coded overdue/soon/ok), and a full history list with edit/delete.
- **To-Do List** — One-off tasks with deadlines, status transitions (Incomplete / Not Started / In Progress / Almost Completed / Completed / Cancelled), counts strip, sortable by date / status / days-left.

### Finance Management (`/finance`)

- **Dashboard** — Pick a date range. See total starting standing (sum of account balances at the start of the period) vs. current standing, total credits / debits in range, plus per-category / per-account / per-payment-type / per-tag breakdowns with proportional bars.
- **Expense Tracker** — All transactions with rich filters (date / account / category / payment type / tag / type / search) and pagination. Add Credit/Debit transactions or **Self Transfers** (which create two linked legs sharing a `TransferGroupId` and clean up together). When filtered by a single account, each row shows its running **Account Standing**. Reports for the selected period in CSV / Excel / PDF.
- **Budget** — Allocate per-category amounts over a custom date range. **Spent** is computed automatically from your transactions; remaining + percent-used + a progress bar (red when over budget). Per-budget detail view + CSV / Excel / PDF download.
- **Settings** — Sub-tabs for Accounts (with opening balance + opening date + active/inactive), Categories (Need / Want / Saving), Payment Types, and Tags (with color picker for visual identity).

## Common operations

```powershell
# Run backend tests
cd backend
dotnet test

# Build production frontend
cd frontend\personal-assistant-ui
ng build --configuration=production
```

## Design notes

- **Soft delete** — Every domain entity inherits `EntityBase` with `IsDeleted`. A SaveChanges interceptor flips `IsDeleted=true` instead of removing rows, and global query filters hide them. History rows in reports therefore stay accurate even after a task is "deleted" from the UI.
- **Tenancy** — Every query filters on `OwnerUserId` taken from the JWT claim, in addition to the `[Authorize]` attribute, as defense-in-depth.
- **Self-transfer model** — A self-transfer is two real `Transaction` rows (a Debit on the source account, a Credit on the destination) joined by a single `TransferGroupId`. The dashboard's category / payment / tag aggregates exclude transfer legs (they net to zero across your own accounts), but the per-account view *includes* them so each account's flow is faithful.
- **Reports** — All four feature areas (Daily, Periodic, To-Do, Finance) push through the same `IReportExportService` taking a generic `ReportTable { Title, Columns, Rows }`. Adding a new report = build a `ReportTable` in your service and route it through the controller.

## Troubleshooting

- **"The Entity Framework tools version is older than the runtime"** — harmless warning. To silence it: `dotnet tool update -g dotnet-ef`.
- **Port already in use** — kill any orphaned `dotnet` or `node` processes, or change ports in `launchSettings.json` (backend) and your `ng serve --port` flag (frontend). Update `apiBaseUrl` in `src/environments/environment.ts` if you change the API port.
- **CORS errors** — the backend allows only `http://localhost:4200` in development. If you serve the frontend from a different origin, edit the CORS policy in `Program.cs`.
- **JWT signing key warnings** — for production, replace the `Jwt:SigningKey` in `appsettings.json` with a long random string (≥ 32 chars) and inject it through user secrets / environment variables.
