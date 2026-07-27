# HANDOVER — Subscription Tracker

**Read this file first if you are a new session continuing this project.** It contains everything needed to resume without re-deriving context.

Last updated: 2026-07-27, after Milestone 8.

## 1. What this project is

An enterprise-grade Subscription Tracker SaaS: .NET 10 Web API backend (Clean Architecture / DDD / CQRS) + Angular frontend (not started yet). Full spec is in the original user prompt at the start of this conversation — re-read it if unsure about scope for a feature. Key non-functional rules from that prompt, still in force:

- No placeholders, no TODOs, no fake/mock services, no demo code.
- Keep the solution buildable at all times; run a full build (and ideally the test suite) after every change before moving on.
- Treat warnings as errors (`Directory.Build.props` enforces this — do not weaken it to make errors go away; fix the root cause or add a narrowly-scoped, justified `NoWarn`).
- Don't ask for approval after every milestone; continue automatically. Only stop for a real blocker or an architectural decision that needs the user's input.
- At the end of each milestone: report what was implemented, files touched, build status, test status, blockers — then continue.

## 2. Current state (end of Milestone 8)

**Milestones 1–8 are complete and committed.** Milestones 9–10 (Angular frontend) are not started. See `TaskList` in this session's harness — if it's not visible to you (new session), the 10-milestone list is reconstructed below in §6.

The app has been **run end-to-end against a real SQL Server LocalDB instance** (not just unit-tested): register → login → create subscription → list subscriptions → cancel subscription → change password → re-login all verified working via `curl`. An automated `WebApplicationFactory`-based integration test locks in this flow (`tests/SubscriptionTracker.Api.IntegrationTests/AuthAndSubscriptionsFlowTests.cs`).

**Test count: 66/66 passing** (45 Domain unit tests, 18 Application unit tests, 3 API integration tests).

**Build: 0 warnings, 0 errors** across all 7 projects.

### Git history

```
(HEAD) feat: add Docker support (Dockerfile, docker-compose, .env.example)
feat: add Quartz.NET background jobs for renewals, expiry, and budget alerts
7bf387a feat: add API layer (JWT auth, permissions, versioning, Swagger, middleware)
7bd9abf feat: add Application layer CQRS (Identity + Subscriptions vertical slices)
3594e80 feat: add EF Core persistence layer (SQL Server)
284c081 feat: add core domain model (Identity, Tenancy, Catalog, Subscriptions, Budgets)
8c592c9 feat: add SharedKernel domain building blocks
62bc5f4 chore: remove template demo files
e6d6361 chore: scaffold Clean Architecture .NET 10 solution
```

Read each commit message in full (`git log -1 <hash>`) before touching related code — they document *why*, not just *what*.

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
dotnet run --project src/Presentation/SubscriptionTracker.Api
# -> Swagger UI: http://localhost:5000/swagger
# -> Health checks: http://localhost:5000/health/live, /health/ready
```

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
- **Identity/**: Register, Login, RefreshToken (rotation), ChangePassword, VerifyEmail, ForgotPassword/ResetPassword (non-enumerable — always returns success even for unknown emails), Logout.
- **Subscriptions/**: CreateSubscription, UpdateSubscription, CancelSubscription, PauseSubscription, ResumeSubscription, GetSubscriptionById, GetSubscriptions (paged/filtered/sorted list).

**Not yet built**: Application-layer CRUD for Category/Tag/PaymentMethod/Budget/Workspace (see §6 — pattern is fully established by Subscriptions, should be fast to replicate).

### Infrastructure layer

- **Persistence/**: `ApplicationDbContext` (implements `IUnitOfWork` and `IApplicationDbContext`), `Configurations/*` (one `IEntityTypeConfiguration<T>` per aggregate — see §5 for EF gotchas you WILL hit if you add new collection properties), `Interceptors/AuditableEntityInterceptor` (sets audit fields + converts hard deletes to soft deletes + cascades soft-delete to loaded child entities), `Interceptors/DomainEventDispatchInterceptor` + `DomainEventNotification<T>` (see §5 bug #2), `Repositories/Repository<TAggregate,TId>` (generic, spec-based) + `SpecificationEvaluator`.
- **Security/**: `PasswordHasher` (PBKDF2-HMACSHA256, 210k iterations — matches ASP.NET Core Identity's algorithm), `JwtTokenService` (HS256, claims include `sub`, `email`, `workspace_id`, one `permission` claim per granted permission code), `JwtOptions`.
- **Notifications/**: `SmtpEmailSender` (MailKit; no-ops with a warning log if `Smtp:Host` isn't configured — this is intentional graceful degradation for dev, not a mock), `SmtpOptions`.
- One initial EF Core migration: `20260726163745_InitialCreate` (in `Persistence/Migrations/`). **Migrations folder has a `.editorconfig` disabling all code analysis** — EF-generated migration files aren't hand-maintained and were tripping `CA1861` on the array-literal `migrationBuilder.CreateIndex(columns: new[] {...})` calls.

### API layer

- JWT bearer auth + a **dynamic permission-policy provider** (`PermissionPolicyProvider`): any `[Authorize(Policy = "Permission:subscriptions:create")]` (or the `[HasPermission("subscriptions:create")]` shorthand attribute) resolves at runtime against the `permission` claims in the token — no need to pre-register every policy.
- `ICurrentUserService` implemented via `IHttpContextAccessor` reading JWT claims.
- Global exception handling: `GlobalExceptionHandler : IExceptionHandler` (unhandled exceptions → generic 500 ProblemDetails, logged with full stack trace via Serilog) + `ResultExtensions.ToActionResult(...)` (business `Result` failures → typed ProblemDetails: `ErrorType.Validation→400`, `NotFound→404`, `Conflict→409`, `Unauthorized→401`, `Forbidden→403`).
- API versioning via URL segment (`/api/v1/...`), Swagger with per-version docs + JWT bearer security scheme, rate limiting (100 req/min per user/IP, fixed window), response compression (Brotli+Gzip), SQL Server health checks at `/health/live` and `/health/ready`, Serilog (console + rolling file), OpenTelemetry (traces + metrics, no exporter destination configured yet — add OTLP endpoint config when you have a collector).
- Controllers: `AuthController`, `SubscriptionsController` (both `api/v1/...`, versioned).

**Not yet built**: controllers for Category/Tag/PaymentMethod/Budget/Workspace management, session management endpoints, 2FA endpoints (domain has the flags — `User.EnableTwoFactor`/`DisableTwoFactor` — but no TOTP generation/validation or API surface yet).

### Background jobs (src/Infrastructure/SubscriptionTracker.Infrastructure/BackgroundJobs)

Quartz.NET, RAM job store (non-clustered — fine for a single instance; switch to a persistent job store if you ever run multiple API replicas, so triggers don't duplicate-fire). Four daily jobs, staggered 15 minutes apart starting 06:00 UTC:

- `RenewalReminderJob` (06:00) — emails owners when `NextRenewalDate - today` matches one of the subscription's `ReminderDaysBeforeRenewal` values. No separate "already sent" tracking table — relies on the date match only occurring once per day per threshold, which is correct as long as the job actually runs daily without gaps.
- `AutoRenewalJob` (06:15) — calls `Subscription.Renew()` for active, auto-renewing subscriptions past their `NextRenewalDate`.
- `ExpireSubscriptionsJob` (06:30) — calls `Subscription.MarkExpiredIfPastRenewalDate()` for non-auto-renewing subscriptions past their date.
- `BudgetAlertJob` (06:45) — estimates each budget's current recurring spend by normalizing every matching subscription's billing cycle to the budget's period (monthly/yearly annualized-then-divided), compares against `Budget.HasExceededThreshold`, emails the workspace owner if crossed. Only sums subscriptions in the *same currency* as the budget — no FX conversion exists in this codebase.

Verified: Quartz scheduler initializes and registers all four jobs/triggers without error at API startup (checked via `dotnet run` + log inspection). **Not verified**: actually triggering a job and observing its output, since that requires either waiting for the cron time or temporarily editing the schedule — the query logic mirrors patterns already proven end-to-end in `GetSubscriptionsQueryHandler`, and the mutating logic (`Renew()`, `MarkExpiredIfPastRenewalDate()`) is covered by the 45 domain unit tests, but the jobs themselves have no dedicated test coverage. If you touch this code, consider adding a quick manual trigger (temporarily change a cron expression to fire in ~1 minute, run the app, check logs, revert) before trusting it.

### Docker (repo root)

`Dockerfile` lives at `src/Presentation/SubscriptionTracker.Api/Dockerfile` but its **build context must be the repo root** (it copies `Directory.Build.props`/`Directory.Packages.props`/the `.slnx` from root, then each project by relative path, for proper Docker layer caching on `dotnet restore`). `docker-compose.yml` at the repo root already sets `build.context: .` and `build.dockerfile: src/Presentation/SubscriptionTracker.Api/Dockerfile` correctly — if you ever run `docker build` by hand instead of via compose, remember `-f src/Presentation/SubscriptionTracker.Api/Dockerfile .` (context `.`, not the Api folder).

`docker-compose.yml` brings up SQL Server 2022 (Express edition, `MSSQL_SA_PASSWORD` required) + the API, wired together with a healthcheck-gated `depends_on` so the API doesn't start until SQL Server responds to `sqlcmd`. Config is injected via env vars using ASP.NET Core's `__` double-underscore section-separator convention (`ConnectionStrings__SubscriptionTrackerDb`, `Jwt__SigningKey`, `Smtp__*`). Copy `.env.example` to `.env` and fill in real values before `docker compose up` — `.env` is gitignored.

The API now applies pending EF Core migrations automatically at startup (`Program.cs`, gated by config key `ApplyMigrationsOnStartup`, default `true`) — added specifically so a fresh `docker compose up` gets a working schema without a separate migration step. Set `ApplyMigrationsOnStartup=false` if you ever move to a controlled/separate migration pipeline (e.g. multi-replica deployments where you don't want every instance racing to apply migrations on boot).

**⚠️ Not build-tested.** Docker is not installed in this environment (checked both the sandboxed Bash tool and the PowerShell host — neither has a `docker` binary), so the Dockerfile/compose file were written carefully by hand-tracing the actual project structure and dependency graph, but **have never actually been run through `docker build`/`docker compose up`**. Before relying on these in any real deployment, run `docker compose up --build` yourself and fix whatever surfaces — given how many subtle bugs turned up when *this* codebase was actually run against a live database (see §5), do not assume the Docker files are bug-free just because they look right on paper. The non-root `appuser` in the runtime image may need explicit write permission if you add file-based logging or local attachment storage (Serilog's file sink is currently configured to write to `logs/` relative to the working directory — verify that's writable by `appuser` in the container, or redirect it to a mounted volume).

## 5. Bugs found and fixed this session (read before touching related code)

These were caught by actually running the app against a live database, not by unit tests (the unit tests all passed while these bugs were live — a reminder that mocked-repository tests don't catch EF Core mapping/query issues). If you're implementing new aggregates/collections/domain events, watch for the same three classes of bug:

1. **EF Core primitive collections require `IList<T>`.** `HashSet<T>`/`SortedSet<T>` backing fields threw `InvalidOperationException: The type 'HashSet<string>' cannot be used as a primitive collection...` at runtime (not at migration-generation time — this only surfaces when you actually track/save an entity). Fix: use `List<T>` backing fields with manual `if (!list.Contains(x)) list.Add(x)` dedup in the domain methods, even when the property is semantically a set. Affected + fixed: `Subscription._tagIds`, `_sharedUserIds`, `_reminderDaysBeforeRenewal`; `Role._permissions`.

2. **MediatR's `IPublisher.Publish` requires `MediatR.INotification`.** Domain events deliberately implement only `SubscriptionTracker.Domain.Common.IDomainEvent` (Domain has zero external dependencies — don't add MediatR there). Fix: `Infrastructure/Persistence/Interceptors/DomainEventNotification.cs` wraps each raw domain event via `Activator.CreateInstance(typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()), domainEvent)` before publishing. **If you add domain event handlers (Milestone 7 will need this for renewal reminders etc.), they must implement `INotificationHandler<DomainEventNotification<TYourEvent>>`, not `INotificationHandler<TYourEvent>`.**

3. **`Repository.GetByIdAsync` used `FindAsync`, which does not eager-load child collections.** Any handler that loads an aggregate by ID and then mutates a child collection (e.g. `ChangePasswordCommandHandler` calling `user.RevokeAllRefreshTokens()`, or `VerifyEmailCommandHandler` calling `user.ConsumeVerificationToken()`) would silently fail to persist the mutation, because EF's change tracker didn't know the collection existed. Fixed two ways together: (a) `Repository.GetByIdAsync` now uses a real `FirstOrDefaultAsync(e => e.Id.Equals(id))` query instead of `FindAsync`; (b) child-collection navigations are configured with `.AutoInclude()` in their `IEntityTypeConfiguration` — `User.RefreshTokens`, `User.VerificationTokens`, `Workspace.Members`, `Subscription.RenewalHistory`, `Subscription.Attachments`. **If you add a new owned/child collection to an aggregate, you must add `.AutoInclude()` to its navigation configuration**, or any handler that loads-then-mutates it will silently no-op.

Also fixed (lower severity, caught before runtime):
- `Workspace.Create` needed an optional pre-assigned `Guid? id` parameter so `RegisterUserCommandHandler` could create a `Role` scoped to a not-yet-persisted `Workspace`'s ID (chicken-and-egg: `Workspace.Create` needs an `ownerRoleId`, but the role needs to know its `workspaceId`).
- `UnitOfWorkBehavior` originally only called `SaveChangesAsync` when `Result.IsSuccess` — this silently dropped state mutations made on the *failure* path (e.g. incrementing `FailedLoginAttempts` before returning "invalid credentials"). Now saves after every command regardless of business-result success/failure (a thrown exception still skips the save, since control never reaches that line).
- FluentValidation's `.GreaterThan(0)` on a nullable int **passes when the value is null** (validators short-circuit on null by convention). `CreateSubscriptionCommandValidator`'s custom-billing-cycle rule needed an explicit `.NotNull()` before `.GreaterThan(0)`. Caught by a unit test, not by manual testing — worth remembering as a general FluentValidation gotcha for any other nullable-conditional rules you write.
- `WorkspacesByMemberUserIdSpecification` filtered on `w.Members.Any(...)` but never called `AddInclude(w => w.Members)` — same root cause as bug #3, fixed both via the spec's explicit include and the `AutoInclude()` on the navigation (belt and suspenders).

## 6. Remaining milestones (not started)

Original 10-milestone plan, for reference:

1. ✅ Solution scaffold
2. ✅ SharedKernel + Domain building blocks
3. ✅ Core domain model
4. ✅ EF Core persistence layer
5. ✅ Application layer CQRS
6. ✅ API layer (auth, versioning, Swagger, middleware)
7. ✅ Background jobs & notifications (Quartz.NET — see §4 "Background jobs" above)
8. ✅ Docker support (Dockerfile + docker-compose — see §4 "Docker" above; **not build-tested**, no Docker available in this environment)
9. ⬜ **Angular frontend scaffold** — standalone components, routing, lazy loading, auth interceptor (attach JWT, handle 401 → refresh-token flow), route guards, core layout, dark/light theme, i18n scaffolding for English/Arabic with RTL. The API's email links already point to specific frontend routes (`/auth/verify-email?userId=...&token=...`, `/auth/reset-password?userId=...&token=...`) — those routes must exist with those exact query param names.
10. ⬜ **Angular features** — Auth pages (login/register/verify-email/forgot-password/reset-password — the API's email links point to `{FrontendBaseUrl}/auth/verify-email?userId=...&token=...` and `/auth/reset-password?userId=...&token=...`, so those exact routes need to exist), Dashboard (KPI cards, charts, upcoming renewals), Subscriptions list/detail/create/edit with filtering, Reports.

Also still outstanding from the original spec, not yet slotted into a milestone — fold into whichever milestone makes sense as you go:
- Application-layer CQRS + API controllers for Category, Tag, PaymentMethod, Budget, Workspace (member invite/accept/remove already has full domain support in `Workspace`, just needs Application handlers + a controller).
- Two-factor authentication (TOTP) — domain flags exist, no implementation.
- Session management UI/API (list/revoke active refresh tokens — `User.RefreshTokens` already tracks `CreatedByIp`/dates, just needs a query + revoke-by-id endpoint).
- File attachment upload (`Subscription.AddAttachment` domain method exists and is fully configured in EF; needs an actual storage backend — local disk or blob storage — and an API endpoint with multipart upload).
- Reports export (PDF/Excel/CSV) — not started.
- Seed data for system roles/permissions (currently every workspace gets its own "Owner" role created ad-hoc at registration with all permissions granted; no seeded "Member"/"Viewer" template roles yet).

## 7. Known non-blocking gaps

- Docker files exist but are unverified — see §4 "Docker" caveat above. Run `docker compose up --build` and fix what breaks before trusting them.
- `Jwt:SigningKey` in `appsettings.Development.json` is a placeholder string — fine for local dev, **must** come from environment variable / secret manager in any real deployment (appsettings.Production.json is gitignored for exactly this reason).
- OpenTelemetry is wired up but has no exporter destination configured (no OTLP collector endpoint in appsettings) — traces/metrics are collected in-process but not shipped anywhere yet.
- `RegisterUserCommandHandler` returns `200 OK` (via `ToActionResult`), not `201 Created` — acceptable (there's no natural "GetUserById" endpoint to `CreatedAtAction` against yet), but worth a second look once a user-profile GET endpoint exists.
- No rate-limit/lockout on the `forgot-password`/`verify-email` endpoints beyond the global 100/min limiter — fine for now, revisit if abuse becomes a concern.

## 8. Workflow notes for continuing

- Follow the **milestone → build → test → commit → next milestone** loop already established. Don't ask for approval between milestones per the original instructions.
- After any Domain-layer change to an aggregate's persisted shape, regenerate the migration: `dotnet ef migrations add <Name> --project src/Infrastructure/SubscriptionTracker.Infrastructure --startup-project src/Infrastructure/SubscriptionTracker.Infrastructure --output-dir Persistence/Migrations`. If the generated `Up()` method is empty, it means no schema change was needed — remove it with `dotnet ef migrations remove` (same project args) rather than leaving a no-op migration in the history.
- Before declaring a milestone done, actually run the app (not just unit tests) for anything touching persistence or the API — see §5 for why. The `dotnet build` + `dotnet test` loop did not catch any of the three real bugs; only running against a live LocalDB did.
- Kill stray `dotnet` processes (see §3) before rebuilding if you've `dotnet run`-tested manually.
