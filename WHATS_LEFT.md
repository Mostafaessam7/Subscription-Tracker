# Subscription Tracker — What's Left

_Last updated: 2026-08-04_

This file is a standalone status snapshot for picking up work in a **new chat**. If you're
starting fresh, read this file first — it tells you what's done, what's genuinely missing, and
what to check before doing more design work.

## How to run it

- Backend: `dotnet run --project src/Presentation/SubscriptionTracker.Api` (listens on
  `http://localhost:5073`, Swagger at `/swagger` in Development).
- Frontend: `cd client && npm install && ng serve` (`http://localhost:4200`).
- CORS in Development accepts any `localhost:<port>` origin, so it doesn't matter which dev
  server/port serves the Angular app (`ng serve`, Visual Studio's SPA proxy, IIS Express, etc.).
- Local test account: `mostafa@subtracker.local` / `DevPass!2026` (not a system admin — the
  `/admin` page will 403 for it; see below).

## Feature gaps (backend/product, not design)

1. **Docker never build-verified.** `Dockerfile` / `docker-compose.yml` exist and were hand-traced
   against the current project structure, but no session on this machine has had a Docker daemon
   available to actually run `docker compose up --build`. Do that first if Docker becomes
   available, and fix whatever breaks on first run.
2. **SignalR live-push not proven end-to-end.** The HTTP negotiate/connect path and the
   `/hubs/notifications` auth (JWT via `access_token` query param) both work, but no session has
   actually triggered a Quartz job (`RenewalReminderJob`, `BudgetAlertJob`) and watched the
   notification arrive live in a connected browser tab. Worth doing once, not urgent.
3. **Frontend test coverage is thin outside `PermissionsService` and `calendar-grid.ts`.** Most
   feature components (subscriptions, budgets, settings, workspace, security, roles, admin,
   reports) have zero `.spec.ts` files. Backend coverage is solid (182/182 across Domain/
   Application/API integration tests); frontend is the gap.
4. **No system admin promoted by default.** `SystemAdmin:BootstrapEmail` isn't set for the local
   test account, so `/admin` returns 403 for it. Set the config key (or `SYSTEM_ADMIN_BOOTSTRAP_EMAIL`
   env var) to a registered email and restart the API to test that page for real.
5. Lower-priority, never-scoped-as-urgent items noted in earlier audits: no billing/plan tier for
   the product itself (it's not sold as a paid SaaS), no usage caps/limits.

## Design (this session's work — what's done)

Every page now shares one consistent visual system: gradient-accented cards (`--gradient-primary`
CSS var), a `.page-header` + `.page-eyebrow` pattern, icon-chip panel headers, hover-lift cards,
and Inter/Manrope typography. Specifically redesigned, in order:

- **Auth pages** (login/register/forgot-password/reset-password/verify-email) — single full-bleed
  Vanta **TOPOLOGY** background (a p5.js effect, not THREE.js — see `AuthBrandPanel` component),
  glass card floating on top, fixed-light card colors regardless of app theme.
- **Shell** (sidenav/topbar) — SVG icon nav, gradient active-state, backdrop-blur topbar.
- **Dashboard** — gradient hero stat card, glow orbs, colored KPI cards, animated gradient bar
  chart, avatar-row upcoming renewals with urgency badges, time-of-day greeting, playful empty
  states with emoji.
- **Subscriptions** (list/detail/form) — card grid with gradient avatars replacing the old plain
  table, hero header on detail page, tag chips, toggle-chip tag picker on the form.
- **Budgets** — animated gradient progress bars for spend vs. limit.
- **Settings, Calendar, Reports, Workspace, Security, Roles, Admin, Audit Log** — all got the
  page-header/panel-icon/gradient-accent treatment for consistency with the pages above.

## Design — what's genuinely still open

1. **Not verified in dark mode.** All of this session's work was eyeballed in the default
   (light-ish) theme via automated browser checks (DOM/text assertions), not a visual pass in
   `data-theme="dark"`. The color tokens (`--color-*`) all have dark-mode values defined in
   `styles.scss`, so it should mostly work, but nobody has actually looked at it.
2. **Not verified in RTL/Arabic.** The app has full en/ar i18n with RTL layout support
   (`:host-context([dir='rtl'])` rules exist for a few components), but none of this session's new
   card grids, hero headers, or icon layouts were checked with the Arabic locale switched on.
   Test by clicking the language toggle in the topbar.
3. **Not verified on mobile/narrow viewports.** The new card grids (`subscription-grid`,
   `budget-grid`, `kpi-grid`) use `auto-fit`/`auto-fill` so they should reflow, but nobody resized
   the browser below ~860px (the sidenav's existing mobile breakpoint) to confirm the new
   components hold up.
4. **No loading skeletons.** Every page still shows a bare `{{ 'common.loading' | translate }}`
   text line while fetching — never upgraded to skeleton placeholders matching the new card shapes.
5. **Icon-only buttons' accessibility labels weren't re-audited.** The shell's icon buttons already
   have `aria-label`s from an earlier session; the newly added inline SVG icons in panel headers,
   KPI cards, etc. are decorative next to text labels so should be fine, but wasn't explicitly
   checked with a screen reader.
6. **Vanta/p5/three are dev dependencies with real weight.** `three`, `p5`, and `vanta` were added
   just for the auth-page background. `three` was pinned to `0.134.0` (down from latest) because
   newer three.js broke `vanta.topology.min.js`'s legacy API usage — don't bump `three` without
   re-testing the auth pages.

## Where to pick up

If asked to keep improving design: dark mode + RTL verification (item 1 and 2 above) are the
highest-value next steps since they're pure regression risk on already-shipped pages, not new
surface area.
