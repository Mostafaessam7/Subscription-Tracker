# Subscription Tracker

An enterprise-grade subscription management SaaS: track recurring subscriptions, get renewal reminders and budget
overspend alerts, manage a shared workspace with teammates, and export reports — built with a .NET 10 Web API
(Clean Architecture / DDD / CQRS) backend and an Angular 22 frontend.

**GitHub**: [github.com/Mostafaessam7/Subscription-Tracker](https://github.com/Mostafaessam7/Subscription-Tracker)

> **⚠️ ARCHIVED — see [PROJECT-STATUS.md](PROJECT-STATUS.md) before doing anything here.**
>
> **Archive note**: this copy lives under `D:\Projects\All\` alongside a separately-numbered
> `D:\Projects\3-Subscription Tracker`, which restructures the same product into a `CleanArch-updated/` (backend) +
> `subscription-tracker-app/` (frontend) layout and has since accumulated its own, later gap-closure history
> (security headers middleware, an Angular 18→22 upgrade path, Playwright E2E tests, CODEOWNERS/issue templates,
> account-deletion + email-confirmation flows not present here). Treat **this** folder as an earlier/parallel
> iteration of the project, not the actively-developed one — check `3-Subscription Tracker` first unless you have a
> specific reason to work from this copy.

> Looking for deep architectural detail, known gotchas, the full feature-by-feature build history, or what's
> genuinely still left to do? See [HANDOVER.md](HANDOVER.md) — it's the working document for anyone (human or AI)
> picking up development on this codebase. This README is a lighter-weight orientation for a first-time human
> contributor.

## Features

- **Subscriptions** — full CRUD, pause/resume/cancel, categories/tags/payment methods, file attachments, renewal
  reminders and auto-renewal via scheduled background jobs, a calendar view of upcoming renewals.
- **Budgets** — per-category or workspace-wide spending limits with live spend tracking and overspend email +
  in-app alerts.
- **Workspaces** — invite teammates (including people who don't have an account yet), assign roles (seeded Owner
  / Member / Viewer, or build your own custom role with a hand-picked permission set), switch between workspaces
  you belong to, manage members.
- **Security** — JWT auth with refresh-token rotation, permission-based authorization (enforced both server-side
  and in the UI), two-factor authentication (TOTP), session management (view/revoke active logins), account
  lockout after repeated failed logins, per-endpoint rate limiting on sensitive auth routes, EF Core global query
  filters for defense-in-depth tenant isolation.
- **Admin & audit** — a cross-tenant system-admin console (list every workspace/user, enable/disable accounts,
  health counts, trigger background jobs on demand) and a full audit log of every command run in the system.
- **Notifications** — in-app notification bell with live SignalR push, alongside the existing email notifications.
- **Reports** — export your subscription list as CSV, Excel, or PDF.
- **i18n** — English and Arabic (with right-to-left layout), dark/light theme.

## Tech stack

| Layer | Technology |
|---|---|
| Backend | .NET 10, ASP.NET Core Web API, EF Core (SQL Server), MediatR (CQRS), FluentValidation, Quartz.NET, Serilog |
| Frontend | Angular 22 (standalone components, signals), Vitest |
| Auth | JWT bearer tokens, permission-based policies, TOTP 2FA |
| Testing | xUnit + NSubstitute + FluentAssertions (backend), Vitest (frontend) |

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org/) 20+ and npm
- SQL Server (LocalDB is fine for local development) or Docker (see below)

## Quick start

### Backend

```bash
# Restore and build
dotnet build

# Apply database migrations (creates the "SubscriptionTracker" database on LocalDB)
dotnet ef database update --project src/Infrastructure/SubscriptionTracker.Infrastructure --startup-project src/Infrastructure/SubscriptionTracker.Infrastructure

# Run the API
dotnet run --project src/Presentation/SubscriptionTracker.Api
```

The API listens on the port configured in `src/Presentation/SubscriptionTracker.Api/Properties/launchSettings.json`
(check that file for the exact port — it varies by profile). In the `Development` environment, Swagger UI is
available at `/swagger`, and health checks at `/health/live` and `/health/ready`.

### Frontend

```bash
cd client
npm install
ng serve
```

Open `http://localhost:4200`. The dev server calls the API directly (see `client/src/environments/environment.ts`
for the configured API base URL) — there's no dev proxy involved.

### Running with Docker

A `Dockerfile` and `docker-compose.yml` are provided at the repo root (bringing up SQL Server + the API together).
Copy `.env.example` to `.env`, fill in real values, then run:

```bash
docker compose up --build
```

**Note:** verified end-to-end (`docker compose up --build`, API health checks, and a live registration against the
containerized SQL Server) — see [HANDOVER.md §7](HANDOVER.md#7-known-non-blocking-gaps) for details.

## Running tests

```bash
# Backend (all projects)
dotnet test

# Frontend
cd client
ng test --watch=false
```

## Project structure

```
src/
  Core/
    SubscriptionTracker.Domain          — domain entities, business rules, zero external dependencies
    SubscriptionTracker.Application     — CQRS commands/queries (MediatR), validation, orchestration
  Infrastructure/
    SubscriptionTracker.Infrastructure  — EF Core, JWT, email, TOTP, file storage, background jobs
  Presentation/
    SubscriptionTracker.Api             — ASP.NET Core Web API controllers
tests/
  SubscriptionTracker.Domain.UnitTests
  SubscriptionTracker.Application.UnitTests
  SubscriptionTracker.Api.IntegrationTests
client/                                 — Angular frontend (see client/README.md)
```

## Configuration & secrets

Configuration follows standard ASP.NET Core layering: `appsettings.json` → `appsettings.{Environment}.json` →
environment variables (using the `Section__Key` double-underscore convention) → command-line arguments. For a real
deployment, supply secrets (`Jwt__SigningKey`, `ConnectionStrings__SubscriptionTrackerDb`, `Smtp__*`) via environment
variables or a secret manager — never commit real secrets to `appsettings.Production.json`. The API refuses to
start in the `Production` environment if `Jwt:SigningKey` is missing or still set to the development placeholder.

### Observability

Serilog handles structured logging out of the box (console + rolling file). OpenTelemetry traces and metrics
(ASP.NET Core, HttpClient, EF Core, .NET runtime counters) are always collected in-process, but nothing ships
anywhere until you set `OpenTelemetry__OtlpEndpoint` (or `OpenTelemetry:OtlpEndpoint` in `appsettings.*.json`) to
an OTLP collector endpoint, e.g. `http://localhost:4317` for a local [OpenTelemetry
Collector](https://opentelemetry.io/docs/collector/) or your APM vendor's OTLP ingest URL. Leave it unset for
local development - there's no collector running by default, and enabling the exporter without one just produces
export-failure warnings in the logs.

### Bootstrapping the first system administrator

Cross-tenant administration (`/api/v1/admin/*` — list every workspace/user, disable/enable accounts, system
health counts) requires the `system_admin` JWT claim, which nothing in the product UI can grant — there's no
"promote to admin" button, deliberately, since that would let any workspace owner grant themselves global access.
Instead, set `SystemAdmin__BootstrapEmail` (or `SystemAdmin:BootstrapEmail` in `appsettings.*.json`) to the email
of an already-registered user; on the next API startup, `SystemAdminSeeder` promotes that account idempotently.
Unset the config key (or change it) once you no longer want new accounts auto-promoted on restart.

## What's next

The project is feature-complete against its original scope plus a full enterprise-readiness audit, and every
previously-open item (LICENSE, CI pipeline, Docker verification, a dashboard KPI aggregate endpoint,
multi-currency budgets, blob storage for attachments) is now done — see
[HANDOVER.md §6-8](HANDOVER.md#6-milestone-status--feature-complete-docker-is-now-verified) for the accurate,
up-to-date list of what's genuinely still open and a few unscoped ideas for further improvement.

## License

MIT — see [LICENSE](LICENSE).
