# Subscription Tracker — What's Left

_Last updated: 2026-08-04 (session 2)_

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
4. **Frontend test coverage — still thin.** Not addressed this session (backend test coverage was
   extended instead — see below). Most feature components (subscriptions, budgets, settings,
   workspace, security, roles, admin, reports) still have zero `.spec.ts` files.
5. **Backend test coverage — extended this session.** 188/188 passing (up from 182): added 6
   integration tests for the new job-trigger endpoint (`AdminControllerTests.cs` — forbidden for
   regular users, 204 for each known job name, 404 for an unknown one).

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
3. **Mobile/narrow viewports — found and fixed a real bug.** The dashboard's decorative glow orbs
   (`22rem`/`16rem` wide, `position: absolute`, `z-index: -1`) weren't contained by their parent,
   so on a narrow viewport they could push the page into horizontal scroll. Added
   `overflow: hidden` to `.dashboard-page` (safe — it already uses `isolation: isolate` for the
   same z-index trick, and no visible content sits outside its bounds). Verified no horizontal
   overflow afterward on dashboard, subscriptions, and budgets.
   **Caveat:** the Browser pane's `resize_window` tool did not actually change the real viewport
   width in this session (`window.innerWidth` stayed stuck regardless of requested size) — a
   tooling limitation, not something to read as "mobile is fully verified visually." The overflow
   check above was done via `scrollWidth`/`clientWidth` comparison at whatever width the tool
   actually provided, which caught the real bug above, but nobody has visually looked at this app
   at true phone width (375px) in an actually-resized viewport.

## Where to pick up

- **Docker**: needs a human at the Docker Desktop window — see item 1 above.
- **Frontend test coverage**: the single biggest remaining gap. Start with the pages that have the
  most non-trivial logic (subscription-list filtering/sorting, budgets spend-percentage
  calculation, dashboard greeting/urgency-badge logic) rather than trivial template-only
  components.
- **True mobile visual pass**: if a Browser pane / device with a real resizable viewport becomes
  available, do one pass at 375px width across all pages — the tooling limitation above means this
  genuinely hasn't happened yet, only the one bug it could statically/structurally catch.
