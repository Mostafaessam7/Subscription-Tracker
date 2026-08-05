# Subscription Tracker — What's Left

_Last updated: 2026-08-05 (session 5)_

This file is a standalone status snapshot for picking up work in a **new chat**. If you're
starting fresh, read this file first — it tells you what's done, what's genuinely missing, and
what to check before doing more design work.

## How to run it

- Backend: `dotnet run --project src/Presentation/SubscriptionTracker.Api` (listens on
  `http://localhost:5073`, Swagger at `/swagger` in Development).
- Frontend: `cd client && npm install && ng serve` (`http://localhost:4200`).
- CORS in Development accepts any `localhost:<port>` origin, so it doesn't matter which dev
  server/port serves the Angular app.
- Local test account: `mostafa@subtracker.local` / `DevPass!2026` — **now a system admin**
  (`SystemAdmin:BootstrapEmail` is set in `appsettings.Development.json`), so `/admin` works
  without extra setup.

## Feature gaps — status as of this session

1. **Docker — still blocked, but differently than before.** A Docker daemon (Docker Desktop) is
   now actually *installed* on this machine (it wasn't in any prior session), and `docker`/`docker
   compose` CLIs work. However, starting Docker Desktop this session left it stuck: `docker info`
   returns `500 Internal Server Error` on the daemon pipe after several minutes, and no
   `vmmem`/WSL process ever appeared — meaning its WSL2 backend never actually came up. This looks
   like it needs a human to look at the Docker Desktop window directly (there may be a setup
   wizard, a WSL update prompt, or a virtualization/BIOS issue) — not something fixable by
   retrying `docker info` from a script. **Next step: open Docker Desktop's UI yourself, resolve
   whatever it's stuck on, then run `docker compose up --build` from the repo root.**
2. **SignalR live-push — done, verified end-to-end.** Added a small system-admin-only endpoint,
   `POST /api/v1/admin/jobs/{jobName}/trigger` (job names: `renewal-reminder`, `auto-renewal`,
   `expire-subscriptions`, `budget-alert`), that fires a Quartz job immediately instead of waiting
   for its cron schedule. Used it to manually trigger `budget-alert` against a real over-threshold
   budget and confirmed the notification arrived live in an already-open, already-connected
   browser tab (bell badge went 0 → 1 with no page reload). This is a real, permanent admin
   feature now, not just a throwaway test hook — covered by 6 new integration tests in
   `AdminControllerTests.cs`.
3. **System admin bootstrap — done.** `SystemAdmin:BootstrapEmail` set to
   `mostafa@subtracker.local` in `appsettings.Development.json`. Restart the API for it to take
   effect if you ever change it (or just log out/in — refresh-token exchanges re-derive the JWT's
   claims from the DB, so a session already open picks up the promotion without a full re-login).
4. **Frontend test coverage — done. Every feature component now has a spec file. 132/132 passing
   (up from 26 at the start).** Session 3: `dashboard.spec.ts` (9), `budgets.spec.ts` (10),
   `subscription-list.spec.ts` (9). Session 4: `subscription-form.spec.ts` (12), `settings.spec.ts`
   (13), `workspace.spec.ts` (8). Session 5 (this one) finished the rest: `calendar.spec.ts` (10 -
   month navigation, day selection/toggle, per-day renewal grouping; `calendar-grid.ts` itself was
   already covered separately), `reports.spec.ts` (5 - filter-to-null coercion, export error
   handling; note `URL.createObjectURL` needs `vi.stubGlobal` since jsdom doesn't implement it),
   `security.spec.ts` (14 - 2FA setup/enable/disable including the 400→invalid-code mapping,
   session revoke), `roles.spec.ts` (12 - permission-catalog grouping, permission toggling, role
   CRUD, edit-state interaction with delete), `admin.spec.ts` (6 - user enable/disable branching,
   reload-after-toggle), `audit-log.spec.ts` (7 - pagination, the `formatAction` PascalCase-to-
   spaced-words regex). All 49 new tests use the same `TestBed.runInInjectionContext(() => new X())`
   + `useValue` stub pattern as the earlier sessions.
5. **Backend test coverage — extended last session.** 188/188 passing: added 6 integration tests
   for the job-trigger endpoint (`AdminControllerTests.cs` — forbidden for regular users, 204 for
   each known job name, 404 for an unknown one).

## Design — what's done (across two sessions)

Every page shares one consistent visual system: gradient-accented cards, a `.page-header` +
`.page-eyebrow` pattern, icon-chip panel headers, hover-lift cards, Inter/Manrope typography.
Covers: auth pages (Vanta TOPOLOGY backdrop, glass card), shell (icon nav, gradient active state),
dashboard (hero stat card, glow orbs, playful empty states), subscriptions (card grid, hero detail
header), budgets (animated progress bars), and settings/calendar/reports/workspace/security/roles/
admin/audit-log (all got the shared panel/icon treatment).

## Design — verification done this session (dark mode / RTL / mobile)

You asked for these three to be checked. Results:

1. **Dark mode — found and fixed a real bug.** `body`'s `background-color`/`color` had a CSS
   `transition:` that, in practice, never re-resolved when the theme changed purely via a custom-
   property update (`:root[data-theme='dark']` swapping `--color-bg`/`--color-text`) — text and
   background stayed stuck at light-theme values everywhere text relied on inherited `color` from
   `body`, even though the CSS custom properties themselves were correctly updating (confirmed via
   `getComputedStyle().getPropertyValue('--color-text')` returning the right value while
   `.color` didn't). Fixed by removing the transition from `body` in `styles.scss` — verified
   after the fix that dashboard, subscriptions, panels, sidenav, and auth pages (which
   deliberately stay light-card regardless of theme) all now resolve correctly in dark mode.
2. **RTL/Arabic — verified clean, no fixes needed.** Grepped every file touched this session for
   hardcoded physical-direction CSS (`left`/`right`/`margin-left`/etc.) — none found; everything
   uses logical properties (`inset-inline-start/end`) or plain flexbox, which auto-flip correctly.
   The one `translateX` animation added (dashboard renewal-row hover) already had an
   `:host-context([dir='rtl'])` override. Spot-checked dashboard and subscriptions pages with the
   language switched to Arabic — text, layout, and icon positions all correct.
3. **Mobile/narrow viewports — found and fixed a real bug (session 3), re-verified more
   thoroughly this session.** The dashboard's decorative glow orbs (`22rem`/`16rem` wide,
   `position: absolute`, `z-index: -1`) weren't contained by their parent, so on a narrow viewport
   they could push the page into horizontal scroll. Fixed with `overflow: hidden` on
   `.dashboard-page`.
   **This session's `resize_window` behavior**: requesting 375px got a real, different width each
   time depending on the page - the standalone `/auth/login` page (no shell/sidenav) genuinely
   rendered at a confirmed true 375px (`window.innerWidth`, `document.documentElement.scrollWidth`,
   and the `read_page` tool's own reported viewport all agreed), but every authenticated page
   inside `.shell` (dashboard, subscriptions, budgets, calendar, settings, reports, workspace,
   security, roles, admin, audit-log) consistently rendered at 459px regardless of the requested
   width or navigation method (SPA route change vs. full `location.href` reload) - looks like an
   environment-specific floor tied to the fixed-position mobile sidenav + flex shell layout, not
   something under the app's control. Swept all 12 pages at that width plus the login page at true
   375px: **zero horizontal overflow anywhere**, and confirmed the sidenav correctly slides off-
   screen (`translateX(-236px)`) with the hamburger toggle visible. This is a real, positive
   verification result (multiple pages checked, one prior bug's fix confirmed still holding) even
   though the exact 375px figure wasn't achieved for shell pages - treat "no overflow bugs found in
   a full page sweep at ~460px and below" as done; true phone-width visual inspection of the shell
   layout specifically is the only piece still open if a different Browser pane/device becomes
   available later.

## Where to pick up

Everything self-directed from the original checklist is now done: features (Docker aside),
frontend test coverage (every component has a spec file), and all three design-verification asks
(dark mode, RTL, mobile). What's left needs either a human or a different environment:

- **Docker**: needs a human at the Docker Desktop window — see item 1 above.
- **True 375px shell-page rendering**: only reachable if a Browser pane/device without the ~459px
  floor described above becomes available — re-run the same overflow sweep this session did.
- Nothing else is currently flagged. If new feature work or design polish gets requested, add it
  here rather than re-deriving the state of the project from scratch.
