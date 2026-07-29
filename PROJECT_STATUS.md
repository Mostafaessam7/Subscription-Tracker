# Project Status — Subscription Tracker

_Last verified: 2026-07-29_

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
- **Workspace switcher** — newly discovered 2026-07-29: `LoginCommandHandler` always logs a
  user into the workspace they *own* (`FirstOrDefault(w => w.OwnerId == user.Id)`) if they own
  one, in preference to any workspace they've been invited into as Member/Viewer. Since every
  registered user automatically owns their own workspace, **the seeded Member/Viewer roles are
  currently unreachable through the normal login flow** — an invited member can accept the
  invitation (`AcceptInvitation` works), but the next login still authenticates them against
  their own workspace, not the one they were invited to. Needs either a workspace picker on
  login or a "switch active workspace" endpoint/UI before Member/Viewer roles have any real
  effect for users who also own a workspace of their own.
- In-app notification center, calendar view of renewals, and a system/super-admin surface are
  all absent — email is the only notification channel, renewals only show as a flat list, and
  there is no cross-tenant admin capability. None of these were previously scoped; flagged here
  as enterprise-checklist gaps for future prioritization.

## 2026-07-29 session: enterprise-readiness audit + fixes

Performed a full code-level audit (not doc review) against an enterprise SaaS checklist
(dashboard, auth, subscriptions, budgets, notifications, roles/permissions, audit logs, admin,
multi-tenancy, integrations, API/testing/logging quality, OpenTelemetry). Findings and fixes:

- **Added an audit log system** (previously ❌ missing entirely): `AuditLogEntry` domain entity,
  `AuditLoggingBehavior` MediatR pipeline behavior that stages an entry for every command
  (success or failure, with actor/workspace/action/entity-id, sensitive fields like
  password/token/secret/code redacted) inside the same `SaveChangesAsync` call as the command
  it describes, a `GetAuditLogsQuery`/`AuditLogsController` (`GET /api/v1/audit-logs`, gated by
  `workspace:manage-settings`), migration `AddAuditLogs`, and a frontend `/audit-log` page.
- **Fixed real UI gap**: the frontend never decoded the JWT's `permission` claims, so every
  authenticated user saw every Create/Edit/Delete button regardless of role. Added
  `PermissionsService` (client-side JWT decode, unit-tested) and gated subscriptions, budgets,
  settings (categories/tags/payment methods), and reports-export UI by the same permission
  codes the backend already enforces. This is UX only — the API was never actually vulnerable,
  since every endpoint already carries its own `[HasPermission(...)]`.
- **Swagger/OpenAPI**: enabled `GenerateDocumentationFile` + `IncludeXmlComments` on the API
  project (previously no XML doc wiring at all); suppressed `CS1591` narrowly since mandating
  doc comments on every existing public member is a separate effort from enabling the pipe.
- **Accessibility**: added `aria-label`s to icon-only topbar buttons, made the subscription
  list's clickable table rows keyboard-operable (`tabindex`, `role="button"`, Enter/Space
  handlers), and added a mobile hamburger nav (sidenav was previously fixed-desktop-only with
  no responsive breakpoint).
- Verified end-to-end in a live browser pass against a real API + LocalDB: registered a fresh
  workspace, confirmed the audit log correctly attributes an authenticated action
  (`Create Category`) to the acting user/workspace while pre-auth actions (Register/Login)
  correctly have no workspace context, and confirmed Owner-role users see all gated buttons
  (proving `PermissionsService` reads real claims, not a hardcoded default).

## Test status (last verified this session)

- Backend: **142/142** automated tests passing (`dotnet test` — 48 Domain, 85 Application, 9 API
  integration; xUnit + NSubstitute + FluentAssertions).
- Frontend: **17/17** Vitest tests passing (`ng test --watch=false`), up from 11 — added
  `PermissionsService` coverage (JWT decode as single-string vs. array claim, missing claim,
  malformed token, unauthenticated).
- Both `dotnet build` and `ng build` are clean, zero warnings.

## Overall completion estimate

**~98%** of scoped functionality is implemented, tested, and documented, plus this session's
audit-log/permission-gating/Swagger/a11y additions. Remaining gaps are either external
(Docker unverifiable in this environment) or intentionally deferred: custom role builder,
PDF export, invite-by-email for unregistered users, the newly-found workspace-switcher gap,
in-app notifications, a calendar view, and a system-admin surface.
