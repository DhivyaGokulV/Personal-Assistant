# Personal Assistant

A self-hosted productivity app with authenticated modules for tasks, finance, assets, time tracking, health, goals, and a client-encrypted password vault. Built with a .NET 10 / EF Core backend and an Angular + Bootstrap frontend.

## Modules

| Module | Status | What's inside |
|---|---|---|
| Task Management | Built | Daily Tasks, Periodic Tasks, To-Do List, reports, task audit archive, typed-delete confirmation |
| Finance Management | Built | Dashboard, Expense Tracker, multi-category Budget, Settings |
| Asset Tracker | Built | Dashboard, Assets, Investments, Liabilities |
| Time Tracker | Built | Calendar-style daily view, CRUD, filters, reports |
| Health & Nutrition | Built | Measurements, workouts, nutrition, water intake, settings, reports |
| Goal Tracker | Built | Goal plans, goals, steps, reports |
| Passwords | Built | Client-side encrypted password groups and entries |
| Notes | Coming soon | Not included in this batch |

## Tech Stack

- Backend: .NET 10, ASP.NET Core Web API, C#, EF Core 10, ASP.NET Identity + JWT, Swagger, Serilog
- Frontend: Angular 21 standalone components, TypeScript, Bootstrap 5, Reactive Forms for new modules, Observables for HTTP calls
- Database: SQLite for Development, SQL Server/Azure SQL for Production; each provider has its own migrations project
- Reports: CSV, Excel, PDF, and JSON via the shared `IReportExportService`
- Theme: responsive light/dark UI with neon border utilities

## Running Locally

Open two terminals.

Backend:

```powershell
cd backend\src\PersonalAssistant.Api
dotnet run --launch-profile http
```

- API: `http://localhost:5024`
- Swagger: `http://localhost:5024/swagger`

Frontend:

```powershell
cd frontend\personal-assistant-ui
npm install
npm.cmd run start -- --port 4200 --host 127.0.0.1
```

- UI: `http://127.0.0.1:4200`

The API auto-applies migrations on startup.

## Environment And Database

Development uses `appsettings.Development.json` and keeps SQLite as the default local database.

Production uses `appsettings.Production.json`, where the provider is SQL Server. Provide these values through environment variables or a secret store:

- `Database__ConnectionStrings__SqlServer`
- `Jwt__SigningKey`
- `Cors__AllowedOrigins__0`, `Cors__AllowedOrigins__1`, etc.

Do not run Production against SQLite.

`appsettings.Production.json` is intentionally ignored by git; use `backend/src/PersonalAssistant.Api/appsettings.Production.template.json` as the tracked starting point.

## Database Provider

Edit `backend/src/PersonalAssistant.Api/appsettings.json`:

```jsonc
"Database": {
  "Provider": "Sqlite",
  "ConnectionStrings": {
    "Sqlite": "Data Source=personal-assistant.db",
    "SqlServer": "Server=(localdb)\\MSSQLLocalDB;Database=PersonalAssistant;Trusted_Connection=True;Encrypt=False;"
  }
}
```

Generate migrations for both providers after model changes:

```powershell
cd backend
dotnet ef migrations add MyChange --project migrations/PersonalAssistant.Migrations.Sqlite --startup-project migrations/PersonalAssistant.Migrations.Sqlite --context AppDbContext
dotnet ef migrations add MyChange --project migrations/PersonalAssistant.Migrations.SqlServer --startup-project migrations/PersonalAssistant.Migrations.SqlServer --context AppDbContext
```

## Security Notes

- All non-auth API endpoints require JWT auth and service queries filter by `OwnerUserId`.
- Domain entities use soft delete through the SaveChanges interceptor.
- Password vault secrets are encrypted in the browser with Web Crypto AES-GCM. New vault metadata supports a wrapped vault key so a configured recovery PIN can reset the master password without storing or displaying the master password.
- The API stores ciphertext, IVs, salts, wrapped-key metadata, and verifier metadata only. If both the master password and recovery PIN are lost, vault contents cannot be recovered.
- Do not put production JWT signing keys or password-vault master passwords in source control.

## Module Routes

- `/tasks`
- `/finance`
- `/assets`
- `/time`
- `/health`
- `/goals`
- `/passwords`

## Recent Improvements

- Dark-theme input placeholders are visible.
- Module accent colors are available through CSS classes like `module-tasks`, `module-finance`, and `module-health`.
- Daily and Periodic task groups/tasks now have display order fields, reorder APIs, and Up/Down controls in the UI.
- Daily and Periodic task create/update/delete actions are written to `TaskArchiveEntries`; completion toggles are intentionally not archived.
- Daily task reports include a consolidated report endpoint with per-date status columns and daily totals.
- Periodic task reports include task history text.
- To-Do status `Incomplete` is replaced by `Just started`; new tasks cannot start as `Completed` or `Cancelled`.
- Finance dashboard account grouping now includes account navigation data, avoiding `(unknown)` account rows.
- Self-transfer transaction updates now update the transfer pair.
- Budget Planner supports multiple category allocations per budget, including per-category spend/remaining summaries and category-aware budget reports.
- Health includes a Water tab with reports.

## Currency

Finance and asset screens display money as Indian rupees using the shared frontend formatter in `finance.models.ts`.

## Verification

```powershell
cd backend
dotnet test tests\PersonalAssistant.Tests\PersonalAssistant.Tests.csproj

cd ..\frontend\personal-assistant-ui
npm.cmd run build
```

Current known frontend build warnings are Angular/Sass diagnostics already present in the project plus a bundle budget warning after adding the new modules.

## Troubleshooting

- CORS: in Development, the API allows `localhost` and `127.0.0.1` origins on any port with credentials, so `localhost:4200`, `127.0.0.1:4200`, and alternate local ports are supported.
- PowerShell npm policy: use `npm.cmd` if `npm.ps1` is blocked by execution policy.
- EF tools warning: if the EF CLI version is older than runtime, update with `dotnet tool update -g dotnet-ef`; the warning is usually harmless for local builds.
- Port already in use: change the Angular port or backend launch profile port, and update `apiBaseUrl` in `src/environments/environment.ts` if the API port changes.
