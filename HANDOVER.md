# HANDOVER — Subscription Tracker

**Read this file first if you are a new session continuing this project.** It contains everything needed to resume without re-deriving context.

**GitHub**: [github.com/Mostafaessam7/Subscription-Tracker](https://github.com/Mostafaessam7/Subscription-Tracker)

Last updated: 2026-08-13. The project is feature-complete against every originally-scoped milestone, every stretch item flagged along the way, and a full enterprise-readiness audit performed on 2026-07-29 and closed out on 2026-08-03 (workspace switcher, tenant isolation, custom role builder, system admin, in-app notifications, renewal calendar, PDF export, invite-by-email for unregistered users, expanded test coverage, OpenTelemetry). A 2026-08-13 session added a `LICENSE` (MIT), a GitHub Actions CI pipeline (`.github/workflows/ci.yml`), and finally got Docker build-verified end-to-end (see §7 for the root cause of the multi-session blocker and the two real Dockerfile bugs it uncovered) — Docker is no longer an open item. See §2 for what's implemented and §6/§7/§8 for what's still genuinely open or in progress.

**Repo location note**: this project has been worked on from more than one machine/path (`F:\My laptob\Project\2-Subscription Tracker` in earlier sessions, `D:\Projects\All\2-Subscription Tracker` currently). Always trust the actual current working directory over any path string in this document.

**Local dev test account**: `mostafa@subtracker.local` / `DevPass!2026` — already promoted to system admin via `SystemAdmin:BootstrapEmail` in `appsettings.Development.json`, so `/admin` works without extra setup.

## 1. What this project is

An enterprise-grade Subscription Tracker SaaS: .NET 10 Web API backend (Clean Architecture / DDD / CQRS) + Angular 22 frontend (standalone components). Full spec is in the original user prompt at the start of the conversation that started this project — re-read it if unsure about scope for a feature. Key non-functional rules from that prompt, still in force:

- No placeholders, no TODOs, no fake/mock services, no demo code.
- Keep the solution buildable at all times; run a full build (and ideally the test suite) after every change before moving on.
- Treat warnings as errors (`Directory.Build.props` enforces this — do not weaken it to make errors go away; fix the root cause or add a narrowly-scoped, justified `NoWarn`).
- Don't ask for approval after every milestone; continue automatically. Only stop for a real blocker or an architectural decision that needs the user's input.
- At the end of each milestone: report what was implemented, files touched, build status, test status, blockers — then continue.

## 2. Current state — feature-complete, audit-closed

**Every originally-scoped milestone, every stretch item flagged along the way, and every finding from the 2026-07-29 enterprise-readiness audit is now implemented, tested, and browser-verified.**

Original scope: Category/Tag/PaymentMethod CRUD, Budget CRUD (with live spend computed from real subscription data), Workspace management (settings, member invite/accept/remove/role-change, pending invitations), session management (list/revoke refresh tokens), 2FA (TOTP setup/enable/disable, enforced at login), subscription attachment upload/download/delete, reports export (CSV, Excel, PDF), and system role seeding (global Member/Viewer templates).

Audit-driven additions (all closed 2026-08-03, see §2b): workspace switcher (Member/Viewer roles are now actually reachable), EF Core global query filters for defense-in-depth tenant isolation, a custom role builder (workspace-defined permission sets, not just Owner/Member/Viewer), cross-tenant system administration (`/admin`, user enable/disable, health counts), in-app notifications with live SignalR push, a renewal calendar view, invite-by-email for users who don't have an account yet, an audit log, client-side permission-based UI gating, rate limiting on sensitive auth endpoints, a production-secrets startup guard, and an OpenTelemetry OTLP exporter. A follow-on session (2026-08-05) also gave the whole app a consistent visual design system, fixed a real dark-mode bug and a real mobile-overflow bug, added a system-admin job-trigger endpoint for on-demand background job runs, and brought frontend test coverage to every feature component.

The full stack has been **run end-to-end for real** (not just unit-tested) many times across sessions — registration → login → 2FA setup/enforcement → subscription CRUD with attachments → budgets with live spend → workspace invite/accept/switch → audit log attribution → permission-gated UI → cross-tenant admin actions → live SignalR notification push after a manually-triggered background job — all verified against a real API + LocalDB with zero console errors. See §5 for the specific bugs these passes caught (including two genuine production bugs: a 500 on Budget delete, and a dark-mode CSS transition bug).

**Test count: 205/205 backend tests passing** (48 Domain, 123 Application, 34 API integration; xUnit + NSubstitute + FluentAssertions) **+ 130/130 frontend tests passing** (Vitest, via `ng test`, up from 26 at the start of the audit — every feature component now has a spec file).

**Build: 0 warnings, 0 errors** across all .NET projects; `ng build` and `ng test` both clean.

### Git history

All of the above is committed. Recent history, newest first (see `git log` for the full list back to the initial scaffold):

```
f27ffd4 test: finish frontend test coverage; re-verify mobile with a proper sweep
f8bf0b8 test: add frontend unit tests for subscription-form, settings, and workspace
8bcd245 test: add frontend unit tests for dashboard, budgets, and subscription list
e38d8f4 feat: add job-trigger admin endpoint, fix dark-mode/mobile-overflow bugs
d058bac docs: add WHATS_LEFT.md status snapshot for picking up work in a new session
20ffb28 feat: finish rolling the design system out across every remaining page
733e49f feat: extend design system to budgets, settings, and calendar
9403e25 feat: redesign subscriptions list/detail/form to match the app's new design language
106f623 feat: give the dashboard a friendlier, more playful personality
4a2131b feat: swap auth backdrop to Vanta WAVES, redesign dashboard as a hero page
4aca642 feat: professional design overhaul with Vanta.js animated auth backdrop
e42927a feat: wire OpenTelemetry OTLP exporter
a68a0a4 test: add integration coverage for previously-untested controllers, fix real budget-delete bug
222078d feat: add invite-by-email for unregistered users
e79e335 feat: add PDF subscriptions report export
e2fc67a feat: add renewal calendar view
4a7b644 feat: add in-app notification center with live SignalR push
c09bdf0 feat: add cross-tenant system administration
b384110 feat: add custom role builder
2ca2cdc feat: add EF Core global query filters for tenant isolation
f2be52a feat: fix workspace switcher end-to-end
3f09e5f feat: add audit logging, permission-based UI gating, and a11y/API-doc fixes
d2d445d feat: add rate limiting, background job tests, production secrets guard, and READMEs
7cf6990 feat: complete remaining stretch scope (Budgets, Workspace, 2FA, sessions, attachments, reports)
b22dfdd feat: add Budget CQRS, Workspace management, and system role seeding
4859218 feat: add Category/Tag/PaymentMethod CRUD and wire into subscription form
06d3732 feat: add Angular Milestone 10 features (auth pages, subscriptions CRUD, dashboard)
6133d33 feat: add Angular frontend scaffold with working auth flow
3a76673 feat: add Docker support (Dockerfile, docker-compose, .env.example)
bf2bdc1 feat: add Quartz.NET background jobs for renewals, expiry, and budget alerts
7bf387a feat: add API layer (JWT auth, permissions, versioning, Swagger, middleware)
7bd9abf feat: add Application layer CQRS (Identity + Subscriptions vertical slices)
3594e80 feat: add EF Core persistence layer (SQL Server)
284c081 feat: add core domain model (Identity, Tenancy, Catalog, Subscriptions, Budgets)
8c592c9 feat: add SharedKernel domain building blocks
62bc5f4 chore: remove template demo files
e6d6361 chore: scaffold Clean Architecture .NET 10 solution
```

Read each commit message in full (`git log -1 <hash>`) before touching related code — they document *why*, not just *what*.

## 2b. The 2026-07-29 audit and its fixes (2026-08-03)

A full code-level audit against an enterprise SaaS checklist (dashboard, auth, subscriptions, budgets, notifications, roles/permissions, audit logs, admin, multi-tenancy, API/testing/logging quality, OpenTelemetry) found 11 gaps, all closed in one autonomous session, committing after each with build+test+live-API verification:

1. **Workspace switcher** — `LoginCommandHandler` always logged a user into the workspace they *own* in preference to any workspace they'd been invited into as Member/Viewer, so those roles were unreachable through the normal login flow even after accepting an invitation. Fixed with `GetMyWorkspacesQuery` + `AuthService.switchWorkspace()` (reuses the refresh-token endpoint's existing but previously-unused target-workspace validation) + a shell dropdown. Verified live: switching actually re-scopes the JWT's permission claims and the UI gates correctly.
2. **Multi-tenant EF Core global query filters** added on Category/Tag/PaymentMethod/Subscription/Budget/Notification/EmailInvitation (defense-in-depth; deliberately *not* on User/Role/Workspace/AuditLogEntry, which have legitimate cross-tenant reads). No-ops when `ICurrentUserService.WorkspaceId` is null (background jobs). Verified with two real tenants via curl: cross-tenant reads correctly return empty/404.
3. **Custom role builder** — `CreateRole`/`UpdateRole`/`DeleteRole`/`GetWorkspaceRoles`/`GetPermissionCatalog`, gated by `workspace:manage-roles`. `GetAssignableRoles` now includes workspace-owned roles so custom roles are immediately invitable. Frontend `/roles` page with a permission checkbox matrix.
4. **System admin / cross-tenant administration** — `User.IsSystemAdmin` + `system_admin` JWT claim, bootstrapped only via `SystemAdmin:BootstrapEmail` config (no self-promotion path exists anywhere in the UI, deliberately). `AdminController`: list all workspaces/users, disable/enable accounts, cross-tenant health counts. Frontend `/admin` page.
5. **In-app notifications** — `Notification` aggregate + `INotificationPublisher` (implemented in the Api layer like `ICurrentUserService`, since SignalR needs the web SDK) called by `RenewalReminderJob`/`BudgetAlertJob` alongside their existing emails. SignalR hub at `/hubs/notifications` (JWT via `access_token` query param — the only way to auth a WebSocket handshake). Notification bell in the shell topbar.
6. **Renewal calendar** — `/calendar` month grid. The grid-building logic is a pure function (`calendar-grid.ts`) specifically so it's unit-testable without Angular DI — this caught a real date-math bug (a renewal on the 1st of a month rendered under the *next* month's same-numbered day) before it shipped.
7. **PDF export** — `GET /reports/subscriptions/pdf` via QuestPDF (free Community license, accepted in the handler's static constructor so it also covers direct-construction unit tests).
8. **Invite-by-email for unregistered users** — new `EmailInvitation` aggregate (mirrors `VerificationToken`'s hashed-token/expiry pattern). `InviteMemberCommandHandler` creates one + emails a sign-up link instead of failing with `UserNotFound`; `RegisterUserCommandHandler` auto-consumes matching invitations at registration, adding the new user as an Invited member (still requires explicit accept, same as the existing-user flow).
9. **Expanded test coverage** — added `WorkspaceControllerTests`/`BudgetsControllerTests`/`CategoriesControllerTests`/`RolesControllerTests`/`AdminControllerTests`/`ReportsControllerTests`/`NotificationsControllerTests` (real `WebApplicationFactory` + LocalDB, not mocked). **Immediately caught a real, previously-unknown production bug**: deleting any Budget returned 500. Root cause: `Budget.Amount` is an owned `Money` value object (`OwnsOne`, same table) — `AuditableEntityInterceptor`'s soft-delete cascade only fixed up child entries implementing `ISoftDeletable`, so the owned `Money` entry stayed `EntityState.Deleted` while its owner flipped to `Modified`, and EF Core throws on that contradiction at `SaveChangesAsync`. No handler-level unit test could ever have caught this (they mock `IRepository` entirely, never touching the real interceptor pipeline). Fixed in `AuditableEntityInterceptor.CascadeSoftDelete` by also flipping owned (non-`ISoftDeletable`) target entries to `Modified`.
10. **OpenTelemetry OTLP exporter** wired (config key `OpenTelemetry:OtlpEndpoint`, unset by default), plus EF Core tracing instrumentation.
11. **Docker verification** — still genuinely blocked at the time: no Docker binary/Docker Desktop available in that environment. See §6/§7 for the current (2026-08-05) status, which has changed but is still blocked.

Two earlier UI-gap fixes from the same audit worth knowing about: the frontend never decoded the JWT's `permission` claims, so every authenticated user saw every Create/Edit/Delete button regardless of role — fixed with a client-side `PermissionsService` (unit-tested) gating buttons by the same permission codes the backend already enforced (the API itself was never actually vulnerable). And Swagger/OpenAPI XML doc generation was enabled, plus `aria-label`s and keyboard operability on icon-only/clickable-row UI, plus a mobile hamburger nav (the sidenav had been fixed-desktop-only with no responsive breakpoint).

## 2c. 2026-08-05 session — design system, real bugs fixed, full frontend test coverage

- **Design system**: every page now shares one consistent visual language — gradient-accented cards, a `.page-header`/`.page-eyebrow` pattern, icon-chip panel headers, hover-lift cards, Inter/Manrope typography, a Vanta TOPOLOGY animated backdrop on auth pages.
- **Real dark-mode bug found and fixed**: `body`'s `background-color`/`color` had a CSS `transition:` that never re-resolved when the theme changed purely via a custom-property update (`:root[data-theme='dark']` swapping `--color-bg`/`--color-text`) — text and background stayed stuck at light-theme values everywhere they relied on inherited `color` from `body`, even though the CSS custom properties themselves were updating correctly. Fixed by removing the transition from `body` in `styles.scss`.
- **Real mobile-overflow bug found and fixed**: the dashboard's decorative glow orbs (`22rem`/`16rem` wide, `position: absolute`, `z-index: -1`) weren't contained by their parent, so on a narrow viewport they could push the page into horizontal scroll. Fixed with `overflow: hidden` on `.dashboard-page`.
- **RTL/Arabic verified clean** — no hardcoded physical-direction CSS anywhere touched this session; everything uses logical properties or flexbox that auto-flips.
- **Mobile sweep**: all 12 authenticated pages plus the login page checked at narrow viewports, zero horizontal-overflow bugs found (one caveat on the exact pixel width achieved — see §7).
- **System-admin job-trigger endpoint**: `POST /api/v1/admin/jobs/{jobName}/trigger` (job names: `renewal-reminder`, `auto-renewal`, `expire-subscriptions`, `budget-alert`) fires a Quartz job immediately instead of waiting for its cron schedule. Used to manually trigger `budget-alert` and confirm the SignalR notification arrived live in an already-open browser tab (bell badge 0 → 1, no reload) — the first time the live-push path was actually exercised end-to-end rather than just the HTTP read/write path. Covered by 6 integration tests in `AdminControllerTests.cs`.
- **Frontend test coverage brought to every feature component** — went from 26 to 132 passing tests across four sessions: `dashboard.spec.ts`, `budgets.spec.ts`, `subscription-list.spec.ts`, `subscription-form.spec.ts`, `settings.spec.ts`, `workspace.spec.ts`, `calendar.spec.ts`, `reports.spec.ts`, `security.spec.ts`, `roles.spec.ts`, `admin.spec.ts`, `audit-log.spec.ts`. All use the same `TestBed.runInInjectionContext(() => new X())` + `useValue` stub pattern. Note: `reports.spec.ts` needs `vi.stubGlobal` for `URL.createObjectURL` since jsdom doesn't implement it.

## 2d. 2026-08-13 session — closed out every remaining item from §8's "further improvement" list

Working through that list in order:

1. **`LICENSE`** (MIT) added at the repo root; README's license section updated to point at it.
2. **CI pipeline** — `.github/workflows/ci.yml`. Backend job runs on `windows-latest` specifically because the integration tests target real SQL Server LocalDB (only ships on the Windows runner image) — `dotnet restore`/`build -c Release`/`test`. Frontend job runs on `ubuntu-latest` — `npm ci`/`ng test --watch=false`/`ng build`.
3. **Docker build-verified end-to-end**, finally, after being blocked across every prior session — see §7 for the full root-cause chain (a crashing Docker Desktop feature, stale sockets only WSL could delete, and two real Dockerfile bugs it then uncovered).
4. **Dashboard KPI aggregate endpoint** — `GET /dashboard/summary` (`GetDashboardSummaryQueryHandler`), computed server-side over every subscription in the workspace instead of the frontend's old client-side computation from a `pageSize=100`-capped list. See §4 API layer for detail. Fixes the >100-subscriptions undercounting gap.
5. **Multi-currency budget support** — `IExchangeRateProvider` (Application abstraction) / `StaticExchangeRateProvider` (Infrastructure), reading a static, manually-maintained rate table from config (`ExchangeRates:BaseCurrency`/`ExchangeRates:Rates`, defaults for USD/EUR/GBP/EGP/CAD/AUD/JPY in `appsettings.json`). Both `GetBudgetsQueryHandler` and `BudgetAlertJob` now convert a cross-currency subscription's normalized spend into the budget's currency instead of skipping it outright; a currency with no configured rate still contributes 0 (same as the old skip-it behavior), so an unconfigured/empty rate table is a safe no-op. **No live FX API is wired up** — real-time rates would need a paid provider and a refresh/caching story, out of scope here; update the config table by hand if rates drift.
6. Docker-blob-storage-for-attachments and SignalR-cron-verification are next in this session's queue — check whether they're marked done above (§2/§6/§7) or still listed as open before assuming either is unstarted.

Test counts after items 4–5: see the running total at the top of §2 (kept current there, not duplicated here to avoid drift between the two numbers).

## 3. How to run it

```bash
# From the repo root F:\My laptob\Project\2-Subscription Tracker

# Build everything
dotnet build

# Run all tests
dotnet test

# Apply migrations to LocalDB (creates the "SubscriptionTracker" database)
dotnet ef database update --project src/Infrastructure/SubscriptionTracker.Infrastructure --startup-project src/Infrastructure/SubscriptionTracker.Infrastructure

# Run the API (Development environment gives you Swagger UI at /swagger)
$env:ASPNETCORE_ENVIRONMENT="Development"   # PowerShell
dotnet run --project src/Presentation/SubscriptionTracker.Api --launch-profile http
# -> Swagger UI: http://localhost:5073/swagger  (see Properties/launchSettings.json — NOT 5000, see §5)
# -> Health checks: http://localhost:5073/health/live, /health/ready
```

```bash
# Frontend, from client/
cd client
npm install         # already run once this session; node_modules is gitignored
ng serve             # -> http://localhost:4200, proxies nothing - calls the API directly via environment.apiBaseUrl
ng build              # production build -> dist/client
ng test --watch=false  # Vitest (Angular 22's default runner, not Karma/Jasmine-in-Chrome)
```

The API's CORS policy (`Cors:AllowedOrigins` in appsettings, defaults to `http://localhost:4200`) must allow whatever origin `ng serve` is actually running on — update it if you change the dev server port.

**Windows-specific gotcha hit repeatedly this session**: when you `dotnet run` an app and then try to `dotnet build`/`dotnet run` again, the previous process holds a file lock on the DLLs and the build fails with `MSB3027`/`MSB3021` ("file is locked by..."). Fix: kill stray `dotnet` processes before rebuilding —

```powershell
Get-Process | Where-Object { $_.ProcessName -eq "dotnet" } | ForEach-Object { Stop-Process -Id $_.Id -Force -ErrorAction SilentlyContinue }
```

Also: `dotnet build <path1> <path2>` (multiple project args) fails with `MSB1008` — build one project at a time, or build the whole solution with no path argument.

The sandboxed Bash tool has outbound network access (NuGet restore works fine over it), but **no SQL Server is reachable from Bash** — that's expected; use PowerShell/the Windows host for anything touching LocalDB (`sqlcmd`, `dotnet ef database update`, running the API against a real DB). LocalDB is installed and confirmed working: `sqlcmd -S "(localdb)\mssqllocaldb" -Q "SELECT @@VERSION"`.

## 4. Architecture map

```
src/Core/SubscriptionTracker.Domain          — zero external dependencies (by design)
src/Core/SubscriptionTracker.Application     — MediatR, FluentValidation, EF Core (LINQ only, for read queries)
src/Infrastructure/SubscriptionTracker.Infrastructure — EF Core, JWT, MailKit, Quartz (not yet used)
src/Presentation/SubscriptionTracker.Api     — ASP.NET Core Web API (controllers, DI composition root)
tests/SubscriptionTracker.Domain.UnitTests
tests/SubscriptionTracker.Application.UnitTests
tests/SubscriptionTracker.Api.IntegrationTests
```

Dependency direction: `Api → Infrastructure → Application → Domain`. `Api` also references `Application` directly (for MediatR `ISender`/DTOs).

### Domain model (src/Core/SubscriptionTracker.Domain)

- **Common/**: `Entity<TId>`, `AggregateRoot<TId>`, `AuditableAggregateRoot<TId>` (audit + soft delete), `ValueObject`, `Result`/`Result<T>`/`Error` (Result pattern — see below), `Specification<T>` (Ardalis-style spec pattern), `IRepository<TAggregate,TId>`, `IUnitOfWork`, `IDomainEvent`/`DomainEvent`.
- **Identity/**: `User` (registration, lockout after 5 failed attempts/15min, 2FA flags, refresh tokens, verification tokens), `Role` (permission codes list), `Permissions` (static catalog of permission code constants), `RefreshToken`, `VerificationToken` (email verification / password reset, hashed + expiring + single-use).
- **Tenancy/**: `Workspace` (aggregate root; owns `WorkspaceMember` collection; invite/accept/remove lifecycle), `WorkspaceSettings` (owned value object: currency/timezone/locale).
- **Catalog/**: `Category`, `Tag`, `PaymentMethod` (all simple workspace-scoped aggregates).
- **Subscriptions/**: `Subscription` (the core aggregate — trial/active/paused/cancelled/expired state machine, `RenewalHistoryEntry` + `SubscriptionAttachment` owned collections, `TagIds`/`SharedUserIds`/`ReminderDaysBeforeRenewal` as **`List<T>`, not HashSet/SortedSet** — see §5 bug #1 for why), `BillingCycle` (value object with per-frequency `CalculateNextRenewalDate`).
- **Budgets/**: `Budget` (threshold-based overspend detection; `HasExceededThreshold(Money spent)` — spend calculation happens in the application layer, not here).

Every domain method that can fail returns `Result`/`Result<T>`, never throws for business-rule violations. Domain events are raised via `RaiseDomainEvent(...)` and dispatched **after** `SaveChanges` succeeds (see Infrastructure interceptors below) — they intentionally have zero dependency on MediatR.

### Application layer (src/Core/SubscriptionTracker.Application)

CQRS via MediatR 12.4.1 (pinned — **do not upgrade past 12.x**, v13+ requires a paid commercial license, see §5).

- **Common/Messaging/**: `ICommand`/`ICommand<T>`/`IQuery<T>` + matching handler interfaces, all wrapping MediatR's `IRequest<Result>`/`IRequest<Result<T>>`.
- **Common/Behaviors/**: pipeline order is `UnhandledExceptionBehavior → ValidationBehavior → UnitOfWorkBehavior → LoggingBehavior` (registered in that order in `DependencyInjection.AddApplication`). `UnitOfWorkBehavior` calls `SaveChangesAsync` after **every** command whether the `Result` is success or business-failure (only a thrown exception skips it — see §5 bug notes on why this matters for e.g. failed-login tracking).
- **Abstractions/**: interfaces Infrastructure/Api implement — `ICurrentUserService`, `IPasswordHasher`, `IJwtTokenService`, `IEmailSender`, `IApplicationDbContext` (read-only `IQueryable<T>` surface used by query handlers to bypass the repository/specification pattern — this is intentional CQRS: commands go through aggregates+repositories, queries hit the DB context directly for projections).
- **Identity/**: Register, Login (now 2FA-aware, see below), RefreshToken (rotation), ChangePassword, VerifyEmail, ForgotPassword/ResetPassword (non-enumerable — always returns success even for unknown emails), Logout, GetCurrentUser (`/auth/me` — email/name/`TwoFactorEnabled`, used by the frontend to know whether to show "set up" vs "disable" 2FA), SetupTwoFactor/EnableTwoFactor/DisableTwoFactor, GetSessions/RevokeSession.
- **Subscriptions/**: CreateSubscription, UpdateSubscription, CancelSubscription, PauseSubscription, ResumeSubscription, GetSubscriptionById, GetSubscriptions (paged/filtered/sorted list), UploadAttachment/DeleteAttachment/DownloadAttachment.
- **Catalog/**: full CRUD for Category, Tag, PaymentMethod — `Create*`/`Update*`/`Delete*`/`Get*` (list-only, no `GetById` — the frontend only ever needs the full list for dropdowns/checklists). Category and Tag creation/rename check a `(WorkspaceId, Name)` uniqueness specification (`CategoryByWorkspaceAndNameSpecification`/`TagByWorkspaceAndNameSpecification`) and return `Error.Conflict` on a duplicate name, matching the DB's unique index (see Infrastructure section). PaymentMethod has no name-uniqueness constraint but does enforce a **"only one default per workspace"** invariant: `CreatePaymentMethodCommandHandler.UnmarkOtherDefaultsAsync` (a `static internal` helper reused by `UpdatePaymentMethodCommandHandler`) loads all currently-default payment methods via `DefaultPaymentMethodByWorkspaceSpecification` and unmarks them before the new/updated one is saved as default. **If you add another payment-method mutation path, route it through that same helper** rather than reimplementing the unmark logic, or the invariant will silently break.
- **Budgets/**: full CRUD (`CreateBudget`/`UpdateBudget`/`DeleteBudget`/`GetBudgets`). `GetBudgetsQuery` computes each budget's **live current spend** by pulling active/trial subscriptions (matching category, if the budget is category-scoped), normalizing each one's billing amount to the budget's period via `BudgetSpendCalculator.NormalizeToPeriod`, and converting cross-currency subscriptions into the budget's currency via `IExchangeRateProvider` (see §2d) — a small static helper (`NormalizeToPeriod`) plus the rate provider are both shared with `BudgetAlertJob` (Infrastructure) so the interactive UI and the overspend-alert email can never disagree on what "current spend" means. `UpdateBudgetCommand` only allows changing `Amount`/`AlertThresholdPercentage` — `Name`/`Period`/`CategoryId` are immutable post-creation (mirrors the same "immutable after creation" pattern as Subscription's billing cycle).
- **Tenancy/**: `GetMyWorkspace` (workspace + resolved member list with names/emails/role names, joined in-handler against `dbContext.Users`/`dbContext.Roles` since `IApplicationDbContext` exposes flat `IQueryable<T>`, not navigable joins across aggregates), `GetAssignableRoles` (global system roles only — see Infrastructure/Seeding below), `GetPendingInvitations` (invitations for the *current* user across *any* workspace, not just their active one — necessary because an invited user's JWT `workspace_id` claim points at whatever workspace they logged in with, which may not be the one they were just invited to), `UpdateWorkspaceSettings`, `InviteMember` (looks up the invitee by email — **the invitee must already have a registered account**; there's no invite-by-email-for-a-not-yet-registered-user flow), `AcceptInvitation` (verifies the accepting user's Id matches the invited member's Id — `Error.Forbidden` otherwise), `ChangeMemberRole`, `RemoveMember`.

Application-layer CRUD for Category/Tag/PaymentMethod/Budget/Workspace/Session/2FA/Attachments is now **fully built** — nothing left in this bucket. The only remaining gap is a custom-role-builder (workspace-defined roles beyond the seeded Member/Viewer templates and the ad-hoc per-workspace Owner role) — see §6.

### Infrastructure layer

- **Persistence/**: `ApplicationDbContext` (implements `IUnitOfWork` and `IApplicationDbContext`), `Configurations/*` (one `IEntityTypeConfiguration<T>` per aggregate — see §5 for EF gotchas you WILL hit if you add new collection properties), `Interceptors/AuditableEntityInterceptor` (sets audit fields + converts hard deletes to soft deletes + cascades soft-delete to loaded child entities), `Interceptors/DomainEventDispatchInterceptor` + `DomainEventNotification<T>` (see §5 bug #2), `Repositories/Repository<TAggregate,TId>` (generic, spec-based) + `SpecificationEvaluator`.
- **Security/**: `PasswordHasher` (PBKDF2-HMACSHA256, 210k iterations — matches ASP.NET Core Identity's algorithm), `JwtTokenService` (HS256, claims include `sub`, `email`, `workspace_id`, one `permission` claim per granted permission code), `JwtOptions`.
- **Notifications/**: `SmtpEmailSender` (MailKit; no-ops with a warning log if `Smtp:Host` isn't configured — this is intentional graceful degradation for dev, not a mock), `SmtpOptions`.
- One initial EF Core migration: `20260726163745_InitialCreate` (in `Persistence/Migrations/`). **Migrations folder has a `.editorconfig` disabling all code analysis** — EF-generated migration files aren't hand-maintained and were tripping `CA1861` on the array-literal `migrationBuilder.CreateIndex(columns: new[] {...})` calls. No new migration was needed for any of this session's features (Budget/Workspace/Session/2FA/Attachments all reuse columns/tables already in the initial migration — `User.TwoFactorEnabled`/`TwoFactorSecret`, `RefreshToken`, `SubscriptionAttachment`, `Role.IsSystemRole` were all already modeled).
- **Security/TotpService** — hand-rolled RFC 6238 TOTP (30s step, 6-digit codes, HMAC-SHA1 — the de-facto standard every authenticator app implements; RFC 6238 permits other hashes but real apps don't support them) plus RFC 4648 Base32 encode/decode for the secret. Takes a `TimeProvider` constructor dependency specifically so it's unit-testable with a fixed clock (see the test suite). `ValidateCode` tolerates ±1 time step (30s) of clock drift. No external TOTP package — the primitives involved are small enough that a dependency wasn't worth it, matching `PasswordHasher`'s hand-rolled PBKDF2 in the same folder.
- **Storage/LocalFileStorageService** — writes subscription attachments to a local directory (`FileStorage:RootPath` config, defaults to `storage/attachments` relative to the working directory). The on-disk filename is always a fresh Guid, never the caller-supplied filename — `ResolveFullPath` runs every stored path through `Path.GetFileName()` before joining it to the root, so even a corrupted/tampered `storagePath` value can't escape the root directory. **This is local-disk only** — if you ever run multiple API replicas or need durability beyond the VM's disk, swap this for a blob-storage implementation of `IFileStorageService` (the interface is already replica/blob-storage-agnostic).
- **Persistence/Seeding/SystemRoleSeeder** — seeds two global (`WorkspaceId = null`, `IsSystemRole = true`) template roles, "Member" and "Viewer", with a fixed sensible permission set each (see the class for the exact lists). Runs once at API startup (`Program.cs`, right after the `ApplyMigrationsOnStartup` migration call) and is idempotent — checks by name+`IsSystemRole` before inserting, safe to run on every boot. These are additive to, not a replacement for, the ad-hoc per-workspace "Owner" role `RegisterUserCommandHandler` still creates at registration.

### API layer

- JWT bearer auth + a **dynamic permission-policy provider** (`PermissionPolicyProvider`): any `[Authorize(Policy = "Permission:subscriptions:create")]` (or the `[HasPermission("subscriptions:create")]` shorthand attribute) resolves at runtime against the `permission` claims in the token — no need to pre-register every policy.
- `ICurrentUserService` implemented via `IHttpContextAccessor` reading JWT claims.
- Global exception handling: `GlobalExceptionHandler : IExceptionHandler` (unhandled exceptions → generic 500 ProblemDetails, logged with full stack trace via Serilog) + `ResultExtensions.ToActionResult(...)` (business `Result` failures → typed ProblemDetails: `ErrorType.Validation→400`, `NotFound→404`, `Conflict→409`, `Unauthorized→401`, `Forbidden→403`).
- API versioning via URL segment (`/api/v1/...`), Swagger with per-version docs + JWT bearer security scheme, rate limiting (100 req/min per user/IP, fixed window), response compression (Brotli+Gzip), SQL Server health checks at `/health/live` and `/health/ready`, Serilog (console + rolling file), OpenTelemetry (traces + metrics, no exporter destination configured yet — add OTLP endpoint config when you have a collector).
- Controllers: `AuthController` (now also `GET /auth/me`, `GET /auth/2fa/setup`, `POST /auth/2fa/enable`, `POST /auth/2fa/disable`), `SubscriptionsController` (now also attachment upload/download/delete), `CategoriesController`, `TagsController`, `PaymentMethodsController`, `BudgetsController`, `WorkspaceController` (`/workspace`, `/workspace/assignable-roles`, `/workspace/invitations`, `/workspace/settings`, `/workspace/members`, `/workspace/members/{id}/accept`, `/workspace/members/{id}/role`, `/workspace/members/{id}`), `SessionsController` (`/sessions`, `DELETE /sessions/{id}`), `ReportsController` (`/reports/subscriptions/csv`, `/reports/subscriptions/excel` — both accept the same `searchTerm`/`categoryId`/`tagId`/`status` filters as `GetSubscriptionsQuery`, factored into a shared `SubscriptionFilters.Apply` helper so the export and the list view can't drift on what "matching the filters" means). All `api/v1/...`, versioned. The catalog/budget controllers are gated by `Permissions.Catalog.{View,Manage}`/`Permissions.Budgets.{View,Manage}` — added to `Permissions.All`, so any **newly-registered** workspace's ad-hoc Owner role picks them up automatically. **Workspaces registered before a given permission was added do not have it** on their stored Owner role (role permission lists are a snapshot taken at registration time, not computed live) — if you hit 403s testing against an old test user, register a fresh one.
- **2FA-aware login**: `LoginCommand` now takes an optional `TotpCode`. If the user has 2FA enabled and no code was supplied, `LoginCommandHandler` returns `Error.Validation("Login.TwoFactorRequired", ...)` **without** touching the failed-login-attempt counter (password was correct — nothing wrong has been guessed yet). If a code was supplied but is wrong, it *does* record a failed login (same lockout protection as a wrong password) and returns `Error.Unauthorized("Login.InvalidTwoFactorCode", ...)`. The frontend's `login.ts` inspects the ProblemDetails `title` for these two exact codes to decide whether to show the code field vs. a hard error.
- **Attachment endpoints** on `SubscriptionsController`: `POST {id}/attachments` (multipart `IFormFile`, `[RequestSizeLimit(10MB)]`, converts to `byte[]` in the controller and hands it to `UploadAttachmentCommand`), `GET {id}/attachments/{attachmentId}` (streams the file back via `File(bytes, contentType, fileName)` — note this bypasses the usual `ToActionResult` ternary since a raw file response isn't a `Result<T>` JSON body), `DELETE {id}/attachments/{attachmentId}`.
- **`DashboardController`** (`GET /dashboard/summary`, gated by `Permissions.Subscriptions.View`) — added 2026-08-13. `GetDashboardSummaryQueryHandler` computes total/active/trial counts, estimated monthly spend (via the same `BudgetSpendCalculator.NormalizeToPeriod` helper Budgets uses, so the number agrees with the rest of the app), the next 5 upcoming renewals within 30 days, and a billing-frequency breakdown — all server-side over *every* subscription in the workspace, not a paged slice. Replaces the frontend's previous client-side computation from `GetSubscriptionsQuery` capped at `pageSize=100`, which undercounted KPIs for any workspace with more than 100 subscriptions. Frontend: `DashboardService.getSummary()` / `core/models/dashboard.models.ts`; `Dashboard` component is now a thin signal-mapping layer over the response instead of doing the aggregation itself.

### Background jobs (src/Infrastructure/SubscriptionTracker.Infrastructure/BackgroundJobs)

Quartz.NET, RAM job store (non-clustered — fine for a single instance; switch to a persistent job store if you ever run multiple API replicas, so triggers don't duplicate-fire). Four daily jobs, staggered 15 minutes apart starting 06:00 UTC:

- `RenewalReminderJob` (06:00) — emails owners when `NextRenewalDate - today` matches one of the subscription's `ReminderDaysBeforeRenewal` values. No separate "already sent" tracking table — relies on the date match only occurring once per day per threshold, which is correct as long as the job actually runs daily without gaps.
- `AutoRenewalJob` (06:15) — calls `Subscription.Renew()` for active, auto-renewing subscriptions past their `NextRenewalDate`.
- `ExpireSubscriptionsJob` (06:30) — calls `Subscription.MarkExpiredIfPastRenewalDate()` for non-auto-renewing subscriptions past their date.
- `BudgetAlertJob` (06:45) — estimates each budget's current recurring spend by normalizing every matching subscription's billing cycle to the budget's period (monthly/yearly annualized-then-divided), converting cross-currency subscriptions via `IExchangeRateProvider` (see §2d), compares against `Budget.HasExceededThreshold`, emails the workspace owner if crossed.

Verified: Quartz scheduler initializes and registers all four jobs/triggers without error at API startup (checked via `dotnet run` + log inspection). **Not verified**: actually triggering a job and observing its output, since that requires either waiting for the cron time or temporarily editing the schedule — the query logic mirrors patterns already proven end-to-end in `GetSubscriptionsQueryHandler`, and the mutating logic (`Renew()`, `MarkExpiredIfPastRenewalDate()`) is covered by the 45 domain unit tests, but the jobs themselves have no dedicated test coverage. If you touch this code, consider adding a quick manual trigger (temporarily change a cron expression to fire in ~1 minute, run the app, check logs, revert) before trusting it.

### Docker (repo root)

`Dockerfile` lives at `src/Presentation/SubscriptionTracker.Api/Dockerfile` but its **build context must be the repo root** (it copies `Directory.Build.props`/`Directory.Packages.props`/the `.slnx` from root, then each project by relative path, for proper Docker layer caching on `dotnet restore`). `docker-compose.yml` at the repo root already sets `build.context: .` and `build.dockerfile: src/Presentation/SubscriptionTracker.Api/Dockerfile` correctly — if you ever run `docker build` by hand instead of via compose, remember `-f src/Presentation/SubscriptionTracker.Api/Dockerfile .` (context `.`, not the Api folder).

`docker-compose.yml` brings up SQL Server 2022 (Express edition, `MSSQL_SA_PASSWORD` required) + the API, wired together with a healthcheck-gated `depends_on` so the API doesn't start until SQL Server responds to `sqlcmd`. Config is injected via env vars using ASP.NET Core's `__` double-underscore section-separator convention (`ConnectionStrings__SubscriptionTrackerDb`, `Jwt__SigningKey`, `Smtp__*`). Copy `.env.example` to `.env` and fill in real values before `docker compose up` — `.env` is gitignored.

The API now applies pending EF Core migrations automatically at startup (`Program.cs`, gated by config key `ApplyMigrationsOnStartup`, default `true`) — added specifically so a fresh `docker compose up` gets a working schema without a separate migration step. Set `ApplyMigrationsOnStartup=false` if you ever move to a controlled/separate migration pipeline (e.g. multi-replica deployments where you don't want every instance racing to apply migrations on boot).

**⚠️ Not build-tested.** Docker is not installed in this environment (checked both the sandboxed Bash tool and the PowerShell host — neither has a `docker` binary), so the Dockerfile/compose file were written carefully by hand-tracing the actual project structure and dependency graph, but **have never actually been run through `docker build`/`docker compose up`**. Before relying on these in any real deployment, run `docker compose up --build` yourself and fix whatever surfaces — given how many subtle bugs turned up when *this* codebase was actually run against a live database (see §5), do not assume the Docker files are bug-free just because they look right on paper. The non-root `appuser` in the runtime image may need explicit write permission if you add file-based logging or local attachment storage (Serilog's file sink is currently configured to write to `logs/` relative to the working directory — verify that's writable by `appuser` in the container, or redirect it to a mounted volume).

### Frontend (client/) — Angular 22, standalone components

```
client/src/app/
  core/
    guards/auth.guard.ts        — authGuard (requires tokens) / guestGuard (redirects if already authed)
    interceptors/auth.interceptor.ts — attaches Bearer token, refreshes on 401 (single-flight via BehaviorSubject), retries
    models/auth.models.ts       — request/response DTOs mirroring the API's C# records exactly (field names matter - System.Text.Json's default is camelCase output, matched here)
    pipes/translate.pipe.ts     — impure pipe wrapping TranslationService.translate(); impure because the dictionary loads async after the pipe would otherwise have already run once
    services/
      auth.service.ts           — signals-based isAuthenticated; register/login/refreshToken/logout all call the real API
      token-storage.service.ts  — localStorage session persistence (access/refresh tokens, expiry, workspaceId, userId)
      theme.service.ts          — dark/light via [data-theme] on <html>, persisted + respects prefers-color-scheme on first load
      translation.service.ts    — loads /i18n/{locale}.json (served from client/public/i18n/), sets [dir]/[lang] on <html> for RTL
  layout/shell/                 — sidenav + topbar (locale/theme toggle, logout), wraps all authenticated routes via router-outlet
  features/
    auth/login/, auth/register/           — real reactive-form pages, fully wired to AuthService
    auth/verify-email/                    — reads userId/token query params, calls VerifyEmail, shows verifying/success/error states
    auth/forgot-password/                 — email form; always shows the same non-enumerable success message (backend never reveals whether the email exists)
    auth/reset-password/                  — reads userId/token query params; shows an "invalid link" state if either is missing before even rendering the form
    dashboard/                            — KPI cards (active/trial counts, estimated monthly spend), upcoming-renewals-in-30-days list, subscriptions-by-billing-frequency breakdown; fetched pre-aggregated from `GET /dashboard/summary` (see §4 API layer) — no more client-side computation or the pageSize=100 undercounting gap that came with it. Breaks down by billing frequency rather than category — a category-name breakdown would be a reasonable follow-on now that Category CRUD exists, just hasn't been built.
    subscriptions/subscription-list/      — table with search/status filter/column sort/pagination, all delegated to GetSubscriptionsQuery query params
    subscriptions/subscription-detail/    — single subscription view + pause/resume/cancel actions (buttons conditionally shown based on current status)
    subscriptions/subscription-form/      — shared create/edit reactive form; in edit mode, billingFrequency/startDate/customIntervalDays/trialEndDate/autoRenewal are disabled because UpdateSubscriptionCommand doesn't accept them (backend treats them immutable post-creation). categoryId/paymentMethodId are now real `<select>` dropdowns and tags are a checkbox checklist (`selectedTagIds` signal, outside the reactive form since Angular reactive forms don't model a plain string-array control cleanly), all populated from `CatalogService` on init.
    subscriptions/subscription-detail/    — also now has an **attachments section**: list with download (blob → `<a download>` → `URL.revokeObjectURL`) and delete, plus a native `<input type="file">` styled as a button that uploads on `change`.
    settings/                             — one page, three sections (Categories/Tags/PaymentMethods), each with a list + a single inline create-or-edit form (an `editingXId: string | null` field on the component toggles the form between create/update mode and swaps the submit button's label) + delete buttons. Deliberately one shared page rather than three separate routes/pages — the CRUD surface for each is small enough that splitting it out would be pure ceremony.
    budgets/                              — same list+inline-form pattern as settings/; cards show live current-spend (from `GetBudgetsQuery`) vs. the budget amount, with a visual "over threshold" state.
    workspace/                            — workspace settings form (currency/timezone/locale), member list with inline role-change `<select>` and remove, an invite form (email + role picked from `GetAssignableRoles`), and — when the current user has any — a "pending invitations" panel above everything else with Accept buttons (populated from `GetPendingInvitations`, which spans *all* workspaces the user is invited to, not just their active one).
    security/                             — two independent panels: 2FA (calls `GET /auth/me` on load to know whether to show "set up" or "currently enabled" + disable form; setup flow shows the raw secret + `otpauth://` URI as text — **no QR code rendering**, since adding a QR library felt like overkill for one screen; users manually enter the secret or copy the URI) and sessions (list + revoke, no "this is your current session" indicator since the access token doesn't carry the refresh-token's Id).
    reports/                              — filter controls (search/category/status, same shape as the subscription list) + two export buttons that call the CSV/Excel endpoints with `responseType: 'blob'` and trigger a browser download via a synthetic `<a>` click.
  app.routes.ts                 — lazy-loaded routes; '' -> dashboard, /auth/* guest-guarded, everything else auth-guarded under the shell (subscriptions routes: '', 'new', ':id/edit', ':id' — order matters, 'new' and ':id/edit' must precede the bare ':id' route)
  app.config.ts                 — provideHttpClient(withInterceptors([authInterceptor])) + provideAppInitializer loading translations before first render
```

i18n dictionaries live in `client/public/i18n/en.json` and `ar.json` (served as static assets, not compiled in) — add new keys to **both** files when adding UI text, and use the `translate` pipe (`{{ 'some.key' | translate }}`) rather than hardcoding strings, or Arabic/RTL support silently degrades for that string. Enum-keyed translations (e.g. `subscriptions.status.1`, `subscriptions.frequency.2`) are looked up by numeric enum value concatenated into the key string — if you add an enum member, add the matching `subscriptions.status.N` / `subscriptions.frequency.N` key to both locale files.

The API's password-reset/email-verification links point at `{FrontendBaseUrl}/auth/verify-email?userId=...&token=...` and `/auth/reset-password?userId=...&token=...` (see `SmtpEmailSender` in the backend) — these routes now exist (Milestone 10) and read the query params exactly as produced. `Smtp:FrontendBaseUrl` in the backend's appsettings must match wherever the Angular app is actually deployed.

Category/Tag/PaymentMethod now have full CRUD on both ends (`core/models/catalog.models.ts` + `core/services/catalog.service.ts` on the frontend, `CategoriesController`/`TagsController`/`PaymentMethodsController` on the backend) — see the `settings/` and `subscriptions/subscription-form/` entries above.

## 5. Bugs found and fixed (read before touching related code)

### From this session (Budget/Workspace/Session/2FA/Attachments/Reports)

No production bugs this time — 132/132 backend tests passed on the first fully-clean run, and the browser pass found zero issues in the app itself. What went wrong was entirely in the **test-writing process**, worth knowing if you extend this test suite:

- **A hand-rolled `IAsyncEnumerable`/`IAsyncQueryProvider` mock for `IApplicationDbContext` doesn't work** — tried to unit-test `ExportSubscriptionsCsvQueryHandler`/`ExportSubscriptionsExcelQueryHandler` (both use `dbContext.Subscriptions.Where(...).Select(...).ToListAsync()`) by mocking `IApplicationDbContext.Subscriptions` with NSubstitute returning a plain `List<T>.AsQueryable()`. EF Core's `ToListAsync` throws `"doesn't implement IAsyncEnumerable"` against a plain LINQ-to-Objects queryable — first attempt fixed this with a custom `TestAsyncEnumerable<T>`/`TestAsyncQueryProvider<T>` pair (a commonly-referenced pattern), but a subtlety in how `IQueryable.Provider` gets re-wrapped on every `CreateQuery` call caused it to silently return **empty results** rather than throw, which is worse than a crash — tests "passed" against zero rows. **Abandoned that approach entirely.** Fixed properly by adding the `Microsoft.EntityFrameworkCore.InMemory` package (test-project-only) and spinning up a real `ApplicationDbContext` with `UseInMemoryDatabase(Guid.NewGuid().ToString())` per test class, seeding it with real aggregate instances via `dbContext.Subscriptions.AddRange(...); await dbContext.SaveChangesAsync();`. **If you need to unit-test a query handler that touches `IApplicationDbContext`, use the real InMemory-backed `ApplicationDbContext`, not an NSubstitute mock of the interface** — the mock can't correctly emulate EF's async query pipeline, and a subtly-wrong hand-rolled provider fails silently rather than loudly.
- Verifying **TOTP end-to-end from the browser tool** required computing a real 6-digit code client-side (no server-side "generate a code" endpoint exists, by design — only an authenticator app should ever hold the secret). Did this via `crypto.subtle` (Web Crypto's HMAC-SHA1) inside `javascript_tool`, matching the exact RFC 6238 construction `TotpService` uses server-side. First two attempts failed with 400 (`EnableTwoFactor.InvalidCode`) purely from **clock drift accumulated across tool round-trips** — computing the code, then several seconds later submitting it, crossed a 30-second step boundary. Fixed by generating the code and clicking Confirm inside the *same* `javascript_tool` call (no round-trip in between). **If you ever need to browser-test a TOTP flow again, compute the code and submit it in one atomic script — don't split "generate code" and "submit code" across separate tool calls.**
- Confirmed once more that native-setter-plus-`dispatchEvent('input')` is the reliable way to fill Angular reactive-form inputs from `javascript_tool` — `element.value = x` directly (without going through the prototype's setter) sometimes leaves the control `ng-pristine`/`ng-invalid` even though the DOM shows the right value. Always use `Object.getOwnPropertyDescriptor(HTMLInputElement.prototype, 'value').set.call(el, value)` before dispatching `input`.

### From the Category/Tag/PaymentMethod CRUD session

No backend bugs surfaced — `dotnet build`/`dotnet test` (82/82) caught everything on the C# side. Two things worth flagging that aren't bugs but are easy to trip on:

- `IApplicationDbContext`'s `IQueryable<T>` properties are explicitly `.AsNoTracking()` (see `ApplicationDbContext.cs`) — you **cannot** mutate an entity fetched through it. `CreatePaymentMethodCommandHandler.UnmarkOtherDefaultsAsync` needed tracked entities to call `UnmarkAsDefault()` + `repository.Update(...)`, so it goes through `IRepository<PaymentMethod,Guid>.ListAsync(spec)` instead of `dbContext.PaymentMethods`. **If a handler needs to read-then-mutate a set of entities (not just project a DTO), use the repository's spec-based `ListAsync`, not `IApplicationDbContext`.**
- The Claude Browser tool's click-doesn't-register issue (see below) also affects **checkboxes**, not just buttons/inputs: `computer{action:"left_click"}` on a checkbox ref toggled the DOM `checked` property without Angular's reactive form ever seeing a `change` event, so a payment method created via the browser test initially saved with `isDefault: false` despite the checkbox appearing checked in the accessibility tree. Fixed the test (not the app) by driving the native `HTMLInputElement.prototype.checked` setter + dispatching a real `change` event via `javascript_tool`, then separately verified the *edit* flow really did flip `isDefault` server-side via a raw `fetch()` against the API. The lesson from §5/earlier sessions generalizes: **any synthetic browser interaction that changes form state (click, type, or check) should be verified against the actual submitted network payload, not just the visual DOM state**, before trusting a "it works" conclusion.

### From Milestone 10 (Angular features)

Caught by actually driving the Angular dev server through a browser against the live API (backend was unchanged this session, so `dotnet test`/`dotnet build` gave no signal on any of these):

- **`client/src/environments/environment.ts` had `apiBaseUrl: 'http://localhost:5000/api/v1'`, but the API's actual dev port (from `launchSettings.json`) is `5073`.** Every API call from the Angular app was silently going to nothing before this fix. This was a pre-existing bug from the Milestone 9 scaffold, not something introduced this session — it just had never been caught because Milestone 9's browser pass apparently used a matching port by coincidence or luck, or wasn't fully retested after a port change. **If you add a new environment file (e.g. `environment.staging.ts`), double check the port against `launchSettings.json`, don't assume 5000.**
- **`CreateSubscriptionController.Create` returns the raw created `Guid` as the response body** (via `Result<Guid>` → `ToCreatedActionResult` → `CreatedAtAction(..., result.Value)`), not a `{ id: ... }` wrapper object. The frontend's `SubscriptionService.create()` originally typed the response as `Observable<{id:string}>` and the create form tried to read `response.id`, which was `undefined`, which then crashed Angular's router with `NG04008: undefined segment`. Fixed by typing `create()` as `Observable<string>` and using the emitted value directly as the id. **Any other command handler that returns a bare scalar (Guid/string/number) via `Result<T>` will serialize the same way — don't assume a wrapper object without checking the actual C# return type.**
- **`GetSubscriptionsQueryValidator` caps `PageSize` at 100** (`InclusiveBetween(1, 100)`). The dashboard originally requested `pageSize: 200` to fetch "all" of a workspace's subscriptions in one call for client-side aggregation, which the backend validator rejected with 400 — silently swallowed by the dashboard's generic error handler (no console error, just a raw "something went wrong" banner with all-zero KPIs). Fixed by capping the dashboard's fetch at 100. **If a workspace ever has >100 subscriptions, the dashboard KPIs will undercount** — this is the reason HANDOVER originally suggested a dedicated aggregate endpoint instead of client-side computation; revisit if that becomes a real constraint.
- Confirmed (again) the `form_input`-doesn't-dispatch-a-real-DOM-event tooling quirk noted below still applies; `computer{action:"left_click"}` on a button ref also intermittently no-ops (e.g. the "Edit" button on the subscription detail page took two attempts, then worked when clicked via `element.click()` through `javascript_tool` instead) — if a click appears to do nothing, fall back to a JS-dispatched `.click()` before concluding the underlying app logic is broken.

### From earlier sessions (Milestones 1-9)

These were caught by actually running the app against a live database, not by unit tests (the unit tests all passed while these bugs were live — a reminder that mocked-repository tests don't catch EF Core mapping/query issues). If you're implementing new aggregates/collections/domain events, watch for the same three classes of bug:

1. **EF Core primitive collections require `IList<T>`.** `HashSet<T>`/`SortedSet<T>` backing fields threw `InvalidOperationException: The type 'HashSet<string>' cannot be used as a primitive collection...` at runtime (not at migration-generation time — this only surfaces when you actually track/save an entity). Fix: use `List<T>` backing fields with manual `if (!list.Contains(x)) list.Add(x)` dedup in the domain methods, even when the property is semantically a set. Affected + fixed: `Subscription._tagIds`, `_sharedUserIds`, `_reminderDaysBeforeRenewal`; `Role._permissions`.

2. **MediatR's `IPublisher.Publish` requires `MediatR.INotification`.** Domain events deliberately implement only `SubscriptionTracker.Domain.Common.IDomainEvent` (Domain has zero external dependencies — don't add MediatR there). Fix: `Infrastructure/Persistence/Interceptors/DomainEventNotification.cs` wraps each raw domain event via `Activator.CreateInstance(typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent)` before publishing. **If you add domain event handlers (Milestone 7 will need this for renewal reminders etc.), they must implement `INotificationHandler<DomainEventNotification<TYourEvent>>`, not `INotificationHandler<TYourEvent>`.**

3. **`Repository.GetByIdAsync` used `FindAsync`, which does not eager-load child collections.** Any handler that loads an aggregate by ID and then mutates a child collection (e.g. `ChangePasswordCommandHandler` calling `user.RevokeAllRefreshTokens()`, or `VerifyEmailCommandHandler` calling `user.ConsumeVerificationToken()`) would silently fail to persist the mutation, because EF's change tracker didn't know the collection existed. Fixed two ways together: (a) `Repository.GetByIdAsync` now uses a real `FirstOrDefaultAsync(e => e.Id.Equals(id))` query instead of `FindAsync`; (b) child-collection navigations are configured with `.AutoInclude()` in their `IEntityTypeConfiguration` — `User.RefreshTokens`, `User.VerificationTokens`, `Workspace.Members`, `Subscription.RenewalHistory`, `Subscription.Attachments`. **If you add a new owned/child collection to an aggregate, you must add `.AutoInclude()` to its navigation configuration**, or any handler that loads-then-mutates it will silently no-op.

Also fixed (lower severity, caught before runtime):
- `Workspace.Create` needed an optional pre-assigned `Guid? id` parameter so `RegisterUserCommandHandler` could create a `Role` scoped to a not-yet-persisted `Workspace`'s ID (chicken-and-egg: `Workspace.Create` needs an `ownerRoleId`, but the role needs to know its `workspaceId`).
- `UnitOfWorkBehavior` originally only called `SaveChangesAsync` when `Result.IsSuccess` — this silently dropped state mutations made on the *failure* path (e.g. incrementing `FailedLoginAttempts` before returning "invalid credentials"). Now saves after every command regardless of business-result success/failure (a thrown exception still skips the save, since control never reaches that line).
- FluentValidation's `.GreaterThan(0)` on a nullable int **passes when the value is null** (validators short-circuit on null by convention). `CreateSubscriptionCommandValidator`'s custom-billing-cycle rule needed an explicit `.NotNull()` before `.GreaterThan(0)`. Caught by a unit test, not by manual testing — worth remembering as a general FluentValidation gotcha for any other nullable-conditional rules you write.
- `WorkspacesByMemberUserIdSpecification` filtered on `w.Members.Any(...)` but never called `AddInclude(w => w.Members)` — same root cause as bug #3, fixed both via the spec's explicit include and the `AutoInclude()` on the navigation (belt and suspenders).
- **No CORS policy existed at all.** Never surfaced until the Angular dev server actually tried to call the API from a different origin — the browser blocked every request. Fixed by adding a `Cors:AllowedOrigins`-configurable policy (`DependencyInjection.AddCors`/`FrontendCorsPolicy`, defaults to `http://localhost:4200`) and `app.UseCors(...)` in the pipeline (must come before `UseAuthentication`/`UseAuthorization`). If you deploy the frontend to a different origin, add it to `Cors:AllowedOrigins` in the relevant `appsettings.*.json` or the environment won't be reachable — this class of bug is invisible to any test that doesn't literally run a browser against the API.
- The Claude Browser tool's synthetic mouse clicks and `form_input` DOM-value-setting were unreliable in this environment (clicks sometimes didn't register on the first attempt; `form_input` set the DOM `.value` without dispatching a real `input` event, so Angular's reactive forms never saw the change and `form.invalid` stayed `true`, silently no-opping `submit()`). This is a tooling quirk, not an app bug — worth knowing if you hit the same "nothing happens on click" symptom: verify with `javascript_tool` by checking `input.className` for `ng-valid`/`ng-dirty`, and if needed drive the native `HTMLInputElement.prototype.value` setter + dispatch a real `Event('input', {bubbles:true})` yourself to confirm the underlying app logic is correct independent of the input tool.

## 6. Milestone status — feature-complete; Docker is now verified

Original 10-milestone plan, all done. Every stretch item ever flagged (Category/Tag/PaymentMethod CRUD, Budget CRUD, Workspace management, session management, 2FA, attachment upload, reports export in CSV/Excel/PDF, system role seeding) is done. Every 2026-07-29 audit finding (workspace switcher, tenant isolation, custom role builder, system admin, in-app notifications, renewal calendar, invite-by-email for unregistered users, audit log, permission-gated UI, rate limiting, production-secrets guard, OpenTelemetry) is done — see §2/§2b/§2c for detail on each.

**Genuinely still open** (real gaps, not blocking normal development):
- **True 375px shell-page rendering** — the 2026-08-05 mobile sweep found zero overflow bugs across every authenticated page, but every page inside `.shell` consistently rendered at ~459px regardless of the requested viewport width (looks like a Browser-pane/environment floor tied to the fixed-position mobile sidenav + flex shell layout, not something under the app's control). The standalone `/auth/login` page (no shell) did render at a true 375px with zero issues. Re-run the same sweep if a different Browser pane/device becomes available.

## 7. Known non-blocking gaps

- **Docker — now verified end-to-end (2026-08-13)**, after being blocked across every prior session. Full root cause, in case it recurs on another machine/profile:
  1. Docker Desktop's actual install lives at `%LOCALAPPDATA%\Programs\DockerDesktop\Docker Desktop.exe` on this machine — not the `C:\Program Files\Docker\...` path earlier sessions checked, which is why they concluded it "wasn't installed."
  2. On launch it crash-looped instantly. `%LOCALAPPDATA%\Docker\log\host\com.docker.backend.exe.log` showed: `backend crashed ... initializing Inference manager: listening on unix://.../Docker/run/dockerInference: remove ...: The file cannot be accessed by the system.` The "Docker AI" (Inference manager) feature was failing to bind its socket and taking the whole backend down with it. Disabled it by setting `"EnableDockerAI": false` in `%APPDATA%\Docker\settings-store.json`.
  3. That still left the backend crashing on **stale AF_UNIX socket files** (Windows reparse points) at `%LOCALAPPDATA%\Docker\run\{dockerInference,dockerEthernetVfkit,userAnalyticsOtlpHttp.sock}` and `%LOCALAPPDATA%\docker-secrets-engine\engine.sock`, left behind by the crash loop. **Windows-native tools cannot delete these** — Explorer, PowerShell `Remove-Item`, and `fsutil reparsepoint delete` all fail with `ERROR_CANT_ACCESS_FILE`/1920. The fix: delete them from inside a running WSL distro instead (`wsl -d Ubuntu -- rm -f '/mnt/c/Users/<user>/AppData/Local/Docker/run/<name>'`) — WSL's drvfs view can unlink what Win32 can't.
  4. After that, `docker info` returned a healthy `Server:` section and `docker compose up --build` ran cleanly, catching **two real Dockerfile bugs** (fixed in `src/Presentation/SubscriptionTracker.Api/Dockerfile`, never caught before because the build had never completed): (a) `mcr.microsoft.com/dotnet/aspnet:10.0`'s slim base doesn't ship the `adduser`/`addgroup` package — added `adduser` to the existing `apt-get install` line alongside `curl`; (b) that same base image already has a built-in `ubuntu` user/group at UID/GID 1000, so the Dockerfile's hardcoded `--gid 1000`/`--uid 1000` collided (`fatal: The GID '1000' is already in use`) — dropped the explicit ids and let `adduser`/`addgroup` pick free ones instead.
  5. Verified live: `docker compose up --build` → both containers reach Docker's own `healthy` state → `GET /health/live` and `/health/ready` both `200` from the host → `POST /api/v1/auth/register` against the containerized API actually created a user + workspace row in the containerized SQL Server (migrations-on-startup confirmed working end-to-end) → `docker compose down` cleaned up. No lingering containers/volumes from this session beyond the named `sqlserver-data` volume compose manages normally.
  - If you hit the same crash-loop on a fresh machine, check `EnableDockerAI` and the `run`-directory stale-socket pattern above before assuming it's unfixable — it looks identical to a "WSL2 backend never comes up" problem from `docker info` alone, but it's actually this specific bug, not a VM/virtualization issue.
- `Jwt:SigningKey` in `appsettings.Development.json` is a placeholder string — fine for local dev; a real deployment **must** supply it via environment variable / secret manager (`ProductionSecretsGuard` now refuses to start the API in `Production` if it's missing or still the placeholder — see §2b).
- OpenTelemetry is wired up (OTLP exporter, config key `OpenTelemetry:OtlpEndpoint`) but unset by default — traces/metrics are collected in-process but not shipped anywhere until you point it at a real collector.
- `RegisterUserCommandHandler` returns `200 OK` (via `ToActionResult`), not `201 Created` — acceptable (there's no natural "GetUserById" endpoint to `CreatedAtAction` against yet), but worth a second look once a user-profile GET endpoint exists.
- Sensitive auth endpoints (`forgot-password`, `reset-password`, `verify-email`) now have dedicated rate limiting (5 requests / 15 minutes per IP, on top of the global 100/min limiter) — see `RateLimitingTests.cs`.
- `FileStorage:RootPath` (attachments) defaults to a relative `storage/attachments` path — fine for a single-instance deployment, but won't survive a container restart/redeploy unless mounted as a persistent volume, and won't work at all across multiple API replicas (each would have its own local disk). Swap `LocalFileStorageService` for a blob-storage implementation of `IFileStorageService` before running more than one replica.
- The `/auth/2fa/setup` → `/auth/2fa/enable` flow hands the raw TOTP secret back to the client twice (once in the setup response, once implicitly when the client re-submits it to enable) rather than holding server-side state between the two calls. This is a deliberate simplicity trade-off (no extra "pending 2FA setup" table/cache needed) and is safe *as long as the connection is HTTPS in production* — the secret is only ever transmitted over the wire, never logged. Don't add logging that captures request bodies on these two endpoints.
- No true end-to-end proof that the SignalR live-push fires from a *cron-scheduled* job run (only from the manually-triggered admin job-trigger endpoint, which exercises the same code path but isn't the same as waiting for the actual schedule).

## 8. Ideas for further improvement (not yet scoped or requested)

None of these are blocking or currently requested — listed here as candidates if you're looking for next work:

- ~~Add a `LICENSE` file~~ — done, MIT, see repo root.
- ~~CI pipeline (GitHub Actions)~~ — done, see `.github/workflows/ci.yml` (backend build+test on `windows-latest` for LocalDB, frontend `ng test`/`ng build` on `ubuntu-latest`).
- ~~Multi-currency support for budgets~~ — done, see §2d.
- **Blob storage for attachments** — in progress this session, see §2d once landed.
- ~~A dedicated aggregate/reporting endpoint for dashboard KPIs~~ — done, `GET /dashboard/summary` (see §4 API layer).
- **Persistent (non-RAM) Quartz job store** — fine for a single instance today; needed before running multiple API replicas so triggers don't duplicate-fire. Not tackled this session — genuinely low priority until multi-replica deployment is a real plan.

## 8. Workflow notes for continuing

- Follow the **milestone → build → test → commit → next milestone** loop already established. Don't ask for approval between milestones per the original instructions — but note that with every milestone and every previously-identified stretch item now done, there is no more implied next work; the next task should come from an explicit user ask, not an inferred backlog.
- After any Domain-layer change to an aggregate's persisted shape, regenerate the migration: `dotnet ef migrations add <Name> --project src/Infrastructure/SubscriptionTracker.Infrastructure --startup-project src/Infrastructure/SubscriptionTracker.Infrastructure --output-dir Persistence/Migrations`. If the generated `Up()` method is empty, it means no schema change was needed — remove it with `dotnet ef migrations remove` (same project args) rather than leaving a no-op migration in the history. (None of this session's features needed a migration — see §4 Infrastructure.)
- Before declaring anything done, actually run the app (not just unit tests) for anything touching persistence or the API — see §5 for why. Across every session on this project, the `dotnet build` + `dotnet test` loop has never once caught a real integration bug on its own; only actually running a browser against the live API has.
- Kill stray `dotnet` processes (see §3) before rebuilding if you've `dotnet run`-tested manually. Same applies to stray `node`/`ng serve`/vite processes on the frontend side if you're iterating quickly.
- When testing the frontend through the Claude Browser tool: prefer driving forms via `javascript_tool` with the native-setter-plus-`dispatchEvent` pattern (see §5) over `computer{action:"left_click"}`/`form_input`, which have repeatedly proven flaky in this environment across many sessions. For any flow with a time-sensitive component (TOTP codes), compute and submit within a single `javascript_tool` call to avoid clock drift from tool round-trip latency.
- Don't duplicate business logic client-side that the backend already validates (e.g. billing-cycle/reminder-day rules, TOTP code format) — call the API and surface its `ProblemDetails` errors, matching the pattern established in `login.ts`/`register.ts` and carried through every feature added since.
- The Category/Tag/PaymentMethod/Budget vertical slices are a clean template for any future simple CRUD aggregate: one DTO + one internal `*Projections` expression per entity, one folder per command/query under `Application/<Area>/<Entity>/<Operation>/`, a thin `*Controller` with `[HasPermission(...)]` per action, and (for entities with a uniqueness constraint) a `Specification<T>` for the duplicate-name check. No changes needed to `DependencyInjection.AddApplication` — MediatR/FluentValidation registration is assembly-scan based, so new handlers/validators just need to exist in the right namespace.
- If you add a new EF-backed query handler and want to unit-test it, use a real `ApplicationDbContext` with `Microsoft.EntityFrameworkCore.InMemory` (already referenced by `SubscriptionTracker.Application.UnitTests`), not an NSubstitute mock of `IApplicationDbContext` — see §5 for why the mock approach silently fails.
