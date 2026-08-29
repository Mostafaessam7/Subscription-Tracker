# Project Status — Subscription Tracker (ARCHIVED ITERATION)

> Last updated: 2026-08-29. This file describes **this repo only**. Every project in the workspace
> has its own status file; nothing here carries over to another.

---

## ⚠️ Read this first: this is not the active project

There are **two** Subscription Tracker repos in this workspace:

| | Path | Status |
|---|---|---|
| **Active** | `D:\Projects\3-Subscription Tracker` | The one under development. Work there. |
| **This one** | `D:\Projects\All\2-Subscription Tracker` | Earlier / parallel iteration. Archived. |

They are the same product, restructured. The active copy uses a
`CleanArch-updated/` + `subscription-tracker-app/` layout and has since accumulated its own later
history that **does not exist here**:

- HttpOnly cookie auth with CSRF double-submit (this copy still uses bearer-token transport)
- Angular 18 → 22 upgrade
- Playwright E2E suite, including an axe-core accessibility gate and dialog keyboard tests
- API versioning, health checks, production secrets validation
- Account deletion and email confirmation flows
- The shared `MeCodex/design-system` (Modern Teal theme)

**Do not port fixes from here to there, or maintain both.** If you need Subscription Tracker, use
the active copy.

---

## 1. State of this copy

It is **not broken** — it is simply superseded. Verified 2026-08-29:

- `dotnet build SubscriptionTracker.slnx -c Release` — **0 warnings**, 0 errors
- Domain unit tests — **37/37**
- Application unit tests — **138/138**
- API integration tests — **48/48**
- **223 tests total, 0 failed**

The product itself is substantial: subscriptions with full lifecycle, budgets with overspend
alerts, multi-user workspaces with custom roles and permissions, reminders via scheduled jobs,
calendar view, exports.

Last functional work done here:
- `/auth/register` made account-non-enumerable
- 4 known CVEs patched in transitive frontend dev-dependencies
- Dependency vulnerability gates added to CI
- Dependabot configured

---

## 2. Decisions adopted

| Decision | Detail |
|---|---|
| **This copy is archived, not deleted** | It has real history and a working 223-test suite. Deleting it would discard a complete parallel implementation for no benefit — disk is cheap, and the archive note makes confusion unlikely |
| **No further feature work here** | Everything goes to `3-Subscription Tracker` |
| **No workspace-level rollout applies** | Azure, Key Vault, Redis, App Insights, Sentry and the shared design system are all scoped to active products. None will be wired here |

---

## 3. Still open

Nothing is planned. This repo is intentionally frozen.

The one thing worth knowing: it still **builds and passes clean**, so it remains a usable reference
for how a feature was implemented before the restructure — particularly the workspace/roles model,
which is more developed here than in the active copy.

---

## 4. Known issues / technical debt

- **Bearer-token auth transport.** The active copy moved to HttpOnly cookies specifically because
  a token readable by JavaScript is exposed to XSS. That fix was never applied here, and will not
  be. This is the main reason not to deploy this copy.
- **Duplicated product.** Two repos implement the same product. That is the cost of keeping the
  archive; the archive note in the README and this file are what stop it becoming confusing.

---

## 5. Deliberately deferred

| Item | Why |
|---|---|
| **All of it** | The project is archived. Applying security, design-system, or infrastructure work here would be maintaining two copies of one product — the exact cost the archive decision was meant to avoid |
| **Deleting the repo** | It builds clean, passes 223 tests, and holds a more developed workspace/roles model than the active copy. Worth keeping as a reference; clearly labelled so nobody works in it by mistake |
