# Project Status — Subscription Tracker

_Last verified: 2026-07-28_

This is the authoritative, project-local status file for the Subscription Tracker SaaS
(.NET 10 Web API + Angular 22 frontend). For architectural detail and session history, see
[HANDOVER.md](HANDOVER.md). For onboarding, see [README.md](README.md).

## ✅ Fully production-ready

- **Auth & identity** — registration, login, JWT + refresh-token rotation, email verification,
  forgot/reset password, two-factor authentication (TOTP), active session management
  (view/revoke), account lockout after repeated failed logins.
- **Subscriptions** — full CRUD, pause/resume/cancel, categories/tags/payment methods,
  file attachments, renewal reminder and auto-renewal background jobs (Quartz.NET).
- **Budgets** — per-category and workspace-wide spending limits, live spend tracking,
  overspend email alerts (background job).
- **Workspaces** — invite teammates, role assignment (Owner/Member/Viewer), member management,
  system role seeding on startup.
- **Reports** — CSV and Excel (ClosedXML) export of subscription data.
- **Frontend** — Angular 22 standalone/signals app covering all of the above: auth pages,
  dashboard, subscriptions, budgets, workspace, security, settings, reports. i18n
  (English/Arabic with RTL) and dark/light theme.
- **Security hardening**:
  - Global API rate limiting (100 req/min per user/IP).
  - Per-endpoint rate limiting on sensitive auth endpoints — forgot-password, reset-password,
    verify-email — at 5 requests / 15 minutes per IP, with a dedicated regression test
    (`RateLimitingTests.cs`) asserting the 6th rapid request returns 429.
  - Fail-fast startup guard (`ProductionSecretsGuard`) that refuses to start the API in the
    `Production` environment if `Jwt:SigningKey` is missing or still the checked-in dev
    placeholder — fully unit tested (`ProductionSecretsGuardTests.cs`), including reading a
    real secret through the standard `Jwt__SigningKey` environment-variable convention.
- **Background jobs verified** — all four Quartz jobs (`RenewalReminderJob`, `AutoRenewalJob`,
  `ExpireSubscriptionsJob`, `BudgetAlertJob`) have direct execution tests
  (`BackgroundJobExecutionTests.cs`) against a real EF Core InMemory `ApplicationDbContext`,
  rather than relying on cron timing.
- **Documentation** — repo-root [README.md](README.md) and [client/README.md](client/README.md)
  rewritten with project-specific quick-start, test, and configuration instructions.

## ⚠️ Partially complete / blocked on external access

- **Docker deployment** — `Dockerfile` and `docker-compose.yml` are present at the repo root,
  but have never been build-verified in any development session on this machine because no
  Docker daemon is installed/available (`docker --version` / `docker info` both fail; no
  `docker` binary found). The compose file brings up SQL Server + the API together. **Requires
  a machine with Docker available to verify and fix any first-run issues.**
- **Observability / OpenTelemetry** — no OpenTelemetry exporter (traces/metrics) is wired up.
  Serilog structured logging is in place, but distributed tracing/metrics export is not.
  This is a genuine remaining scope item, not a blocker — it can be picked up whenever desired.

## ❌ Not implemented (known remaining scope, low priority)

- Custom/user-defined role builder (only fixed Owner/Member/Viewer roles exist today).
- PDF export for reports (CSV and Excel are implemented; PDF is not).
- Inviting a workspace member by email who doesn't yet have an account (invites currently
  target existing registered users only).

## Test status (last verified this session)

- Backend: **153/153** automated tests passing (`dotnet test` — xUnit + NSubstitute +
  FluentAssertions across Domain/Application unit tests and API integration tests).
- Frontend: Vitest suite passing (`ng test --watch=false`).
- Both `dotnet build` and the Angular build are clean with no warnings introduced by this
  session's changes.

## Overall completion estimate

**~98%** of scoped functionality is implemented, tested, and documented. The only genuine
blockers are external (no Docker daemon available in this environment) or intentionally
deferred low-priority stretch items listed above.
