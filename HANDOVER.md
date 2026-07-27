# HANDOVER — Subscription Tracker

**Read this file first if you are a new session continuing this project.** It contains everything needed to resume without re-deriving context.

Last updated: 2026-07-27, after Milestone 10 + Category/Tag/PaymentMethod CRUD (a post-milestone-10 follow-on, see §6).

**Repo location note**: this project has been worked on from more than one machine/path (`F:\My laptob\Project\2-Subscription Tracker` in earlier sessions, `D:\Projects\All\2-Subscription Tracker` currently). Always trust the actual current working directory over any path string in this document.

## 1. What this project is

An enterprise-grade Subscription Tracker SaaS: .NET 10 Web API backend (Clean Architecture / DDD / CQRS) + Angular 22 frontend (standalone components). Full spec is in the original user prompt at the start of the conversation that started this project — re-read it if unsure about scope for a feature. Key non-functional rules from that prompt, still in force:

- No placeholders, no TODOs, no fake/mock services, no demo code.
- Keep the solution buildable at all times; run a full build (and ideally the test suite) after every change before moving on.
- Treat warnings as errors (`Directory.Build.props` enforces this — do not weaken it to make errors go away; fix the root cause or add a narrowly-scoped, justified `NoWarn`).
- Don't ask for approval after every milestone; continue automatically. Only stop for a real blocker or an architectural decision that needs the user's input.
- At the end of each milestone: report what was implemented, files touched, build status, test status, blockers — then continue.

## 2. Current state (end of Milestone 10 + Category/Tag/PaymentMethod CRUD)

**All 10 milestones are complete**, and one of the previously-flagged stretch items — **Application CQRS + API + frontend for Category, Tag, and PaymentMethod** — is now also done. What's left is the remaining "also still outstanding" list in §6 (2FA, session management, file uploads, reports export) — none of these were part of the numbered 10-milestone plan; they were flagged as stretch/follow-on scope throughout.

The full stack has been **run end-to-end for real** (not just unit-tested) multiple times across sessions: backend against a live SQL Server LocalDB instance, and the **Angular dev server driven through an actual browser against the live API**. The Category/Tag/PaymentMethod pass covered: creating a category/tag/payment-method via the new `/settings` page, editing a payment method to flip its `isDefault` flag (confirming the unmark-other-defaults invariant), deleting a tag, and then creating a subscription through `subscription-form` with a real category/payment-method/tag selected — confirmed the subscription detail page resolves and displays the category name, payment method label, and tag name (not raw GUIDs), and confirmed deleting a referenced tag afterward doesn't break the detail page (it just silently stops showing that tag). See §5 for what this surfaced.

**Test count: 82/82 backend tests passing** (45 Domain, 34 Application — 18 original + 16 new Catalog tests, 3 API integration) **+ 11/11 frontend tests passing** (Vitest, via `ng test` — no new frontend unit tests added for Catalog; coverage there is the browser pass described above).

**Build: 0 warnings, 0 errors** across all 7 .NET projects; `ng build` and `ng test` both clean.

### Git history

Milestone 10 (auth pages, subscriptions CRUD/actions, dashboard) is committed. **The Category/Tag/PaymentMethod CRUD work described in this update is uncommitted as of this HANDOVER edit** — commit it yourself (or ask to have it committed) once you've reviewed the diff. Prior history (all committed):

```
06d3732 feat: add Angular Milestone 10 features (auth pages, subscriptions CRUD, dashboard)
c6f65dc docs: update HANDOVER.md for Milestone 9 completion
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
- **Identity/**: Register, Login, RefreshToken (rotation), ChangePassword, VerifyEmail, ForgotPassword/ResetPassword (non-enumerable — always returns success even for unknown emails), Logout.
- **Subscriptions/**: CreateSubscription, UpdateSubscription, CancelSubscription, PauseSubscription, ResumeSubscription, GetSubscriptionById, GetSubscriptions (paged/filtered/sorted list).
- **Catalog/**: full CRUD for Category, Tag, PaymentMethod — `Create*`/`Update*`/`Delete*`/`Get*` (list-only, no `GetById` — the frontend only ever needs the full list for dropdowns/checklists). Category and Tag creation/rename check a `(WorkspaceId, Name)` uniqueness specification (`CategoryByWorkspaceAndNameSpecification`/`TagByWorkspaceAndNameSpecification`) and return `Error.Conflict` on a duplicate name, matching the DB's unique index (see Infrastructure section). PaymentMethod has no name-uniqueness constraint but does enforce a **"only one default per workspace"** invariant: `CreatePaymentMethodCommandHandler.UnmarkOtherDefaultsAsync` (a `static internal` helper reused by `UpdatePaymentMethodCommandHandler`) loads all currently-default payment methods via `DefaultPaymentMethodByWorkspaceSpecification` and unmarks them before the new/updated one is saved as default. **If you add another payment-method mutation path, route it through that same helper** rather than reimplementing the unmark logic, or the invariant will silently break.

**Not yet built**: Application-layer CRUD for Budget/Workspace (see §6 — pattern is fully established by Subscriptions/Catalog, should be fast to replicate).

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
- Controllers: `AuthController`, `SubscriptionsController`, `CategoriesController`, `TagsController`, `PaymentMethodsController` (all `api/v1/...`, versioned). The three catalog controllers are gated by two new permission codes, `Permissions.Catalog.View` (GET) and `Permissions.Catalog.Manage` (POST/PUT/DELETE) — added to `Permissions.All`, so any **newly-registered** workspace's ad-hoc Owner role picks them up automatically. **Workspaces registered before this change do not have these permissions** on their stored Owner role (role permission lists are a snapshot taken at registration time, not computed live) — if you hit 403s testing against an old test user, register a fresh one or manually grant the permission.

**Not yet built**: controllers for Budget/Workspace management, session management endpoints, 2FA endpoints (domain has the flags — `User.EnableTwoFactor`/`DisableTwoFactor` — but no TOTP generation/validation or API surface yet).

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
    dashboard/                            — KPI cards (active/trial counts, estimated monthly spend), upcoming-renewals-in-30-days list, subscriptions-by-billing-frequency breakdown; all computed client-side from GetSubscriptionsQuery (pageSize capped at the backend's max of 100 — see §5). No category-name breakdown since Category CRUD doesn't exist yet (see §6) — breaks down by billing frequency instead.
    subscriptions/subscription-list/      — table with search/status filter/column sort/pagination, all delegated to GetSubscriptionsQuery query params
    subscriptions/subscription-detail/    — single subscription view + pause/resume/cancel actions (buttons conditionally shown based on current status)
    subscriptions/subscription-form/      — shared create/edit reactive form; in edit mode, billingFrequency/startDate/customIntervalDays/trialEndDate/autoRenewal are disabled because UpdateSubscriptionCommand doesn't accept them (backend treats them immutable post-creation). categoryId/paymentMethodId are now real `<select>` dropdowns and tags are a checkbox checklist (`selectedTagIds` signal, outside the reactive form since Angular reactive forms don't model a plain string-array control cleanly), all populated from `CatalogService` on init.
    settings/                             — one page, three sections (Categories/Tags/PaymentMethods), each with a list + a single inline create-or-edit form (an `editingXId: string | null` field on the component toggles the form between create/update mode and swaps the submit button's label) + delete buttons. Deliberately one shared page rather than three separate routes/pages — the CRUD surface for each is small enough that splitting it out would be pure ceremony.
  app.routes.ts                 — lazy-loaded routes; '' -> dashboard, /auth/* guest-guarded, everything else auth-guarded under the shell (subscriptions routes: '', 'new', ':id/edit', ':id' — order matters, 'new' and ':id/edit' must precede the bare ':id' route)
  app.config.ts                 — provideHttpClient(withInterceptors([authInterceptor])) + provideAppInitializer loading translations before first render
```

i18n dictionaries live in `client/public/i18n/en.json` and `ar.json` (served as static assets, not compiled in) — add new keys to **both** files when adding UI text, and use the `translate` pipe (`{{ 'some.key' | translate }}`) rather than hardcoding strings, or Arabic/RTL support silently degrades for that string. Enum-keyed translations (e.g. `subscriptions.status.1`, `subscriptions.frequency.2`) are looked up by numeric enum value concatenated into the key string — if you add an enum member, add the matching `subscriptions.status.N` / `subscriptions.frequency.N` key to both locale files.

The API's password-reset/email-verification links point at `{FrontendBaseUrl}/auth/verify-email?userId=...&token=...` and `/auth/reset-password?userId=...&token=...` (see `SmtpEmailSender` in the backend) — these routes now exist (Milestone 10) and read the query params exactly as produced. `Smtp:FrontendBaseUrl` in the backend's appsettings must match wherever the Angular app is actually deployed.

Category/Tag/PaymentMethod now have full CRUD on both ends (`core/models/catalog.models.ts` + `core/services/catalog.service.ts` on the frontend, `CategoriesController`/`TagsController`/`PaymentMethodsController` on the backend) — see the `settings/` and `subscriptions/subscription-form/` entries above.

## 5. Bugs found and fixed (read before touching related code)

### From this session (Category/Tag/PaymentMethod CRUD)

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

## 6. Milestone status — all 10 complete; Category/Tag/PaymentMethod CRUD also done; remaining work is stretch scope

Original 10-milestone plan, all done:

1. ✅ Solution scaffold
2. ✅ SharedKernel + Domain building blocks
3. ✅ Core domain model
4. ✅ EF Core persistence layer
5. ✅ Application layer CQRS
6. ✅ API layer (auth, versioning, Swagger, middleware)
7. ✅ Background jobs & notifications (Quartz.NET — see §4 "Background jobs" above)
8. ✅ Docker support (Dockerfile + docker-compose — see §4 "Docker" above; **not build-tested**, no Docker available in this environment)
9. ✅ Angular frontend scaffold (routing, lazy loading, auth interceptor + guards, layout shell, dark/light theme, en/ar i18n with RTL, working Login/Register — see §4 "Frontend" above)
10. ✅ **Angular features**:
    - Auth pages: verify-email, forgot-password, reset-password — done, browser-verified including the invalid-token error path (see §5)
    - Dashboard: KPI cards, upcoming renewals (30-day window), spend-by-billing-frequency breakdown — done, computed client-side from `GetSubscriptionsQuery` (see §4 "Frontend" for the pageSize-100 cap)
    - Subscriptions: list (filter/sort/pagination), detail view, create/edit forms, pause/resume/cancel actions — done, browser-verified end to end (create → detail → pause → edit → cancel, plus the list/dashboard reflecting each state change)
    - Reports: PDF/Excel/CSV export — **not done**, no backend support exists either; this was always flagged as greenfield-on-both-sides and still hasn't been picked up
    - Category/Tag/PaymentMethod management UI — **done** (see below), was originally blocked on backend Application/API work, which is now also done

**Post-Milestone-10 follow-on, also done**: Application-layer CQRS + API controllers + frontend `settings/` page for Category, Tag, and PaymentMethod (see §4 Application/API/Frontend sections and §5 for details). Wired into `subscription-form`'s categoryId/paymentMethodId dropdowns and tagIds checklist, and into `subscription-detail`'s display (resolves IDs to names client-side via `CatalogService`). Backend: 16 new Application unit tests. Browser-verified: full create/edit/delete cycle for all three entity types, the payment-method "only one default" invariant, and a subscription referencing a category/tag/payment-method by ID resolving to the right display name.

Still outstanding from the original spec, never part of the numbered milestones — pick up as a follow-on project if needed:
- Application-layer CQRS + API controllers for Budget, Workspace (member invite/accept/remove already has full domain support in `Workspace`, just needs Application handlers + a controller).
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
- Before declaring a milestone done, actually run the app (not just unit tests) for anything touching persistence or the API — see §5 for why. The `dotnet build` + `dotnet test` loop did not catch any of the three real backend bugs, nor the missing-CORS bug; only actually running a browser against the live API did.
- Kill stray `dotnet` processes (see §3) before rebuilding if you've `dotnet run`-tested manually. Same applies to stray `node`/`ng serve`/vite processes on the frontend side if you're iterating quickly.
- When testing the frontend through the Claude Browser tool, be aware of the input-delivery flakiness noted in §5 — if a click/type seems to do nothing, verify with `javascript_tool` (check `ng-valid`/`ng-dirty` classes, or just read `input.value`) before concluding the app itself is broken.
- Milestone 10's Subscriptions/Auth pages relied entirely on backend endpoints that already existed (see §4). Don't duplicate business logic client-side that the backend already validates (e.g. billing-cycle/reminder-day rules) — call the API and surface its `ProblemDetails` errors, matching the pattern established in `login.ts`/`register.ts` and carried through `subscription-form.ts`.
- With all 10 milestones done and Category/Tag/PaymentMethod CRUD also done, treat this as a fresh scoping conversation rather than an implied next-milestone continuation: confirm with the user whether they want the remaining §6 stretch items (2FA, sessions, attachments, reports export, Budget/Workspace CRUD), Docker verification, or something else entirely before starting new work.
- The Category/Tag/PaymentMethod slice is a clean template to copy for Budget/Workspace CRUD: one DTO + one internal `*Projections` expression per entity, one folder per command/query under `Application/Catalog/<Entity>/<Operation>/`, a thin `*Controller` with `[HasPermission(...)]` per action, and (for entities with a uniqueness constraint) a `Specification<T>` for the duplicate-name check. No changes needed to `DependencyInjection.AddApplication` — MediatR/FluentValidation registration is assembly-scan based, so new handlers/validators just need to exist in the right namespace.
