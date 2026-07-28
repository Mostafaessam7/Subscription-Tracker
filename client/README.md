# Subscription Tracker — Client

Angular 22 frontend for the Subscription Tracker SaaS (standalone components, signals-based state). See the
[repo-root README](../README.md) for the overall project, and [HANDOVER.md](../HANDOVER.md) for deep architectural
detail.

## Development server

```bash
npm install
ng serve
```

Open `http://localhost:4200`. The app calls the backend API directly — no dev proxy is configured — so the API
must be running too (see the repo-root README's "Quick start" section). The API base URL is set in
`src/environments/environment.ts`; if you change the API's port, update it there.

## Building

```bash
ng build
```

Production build output goes to `dist/client`.

## Running tests

```bash
ng test --watch=false
```

Uses Vitest (Angular 22's default test runner), not Karma/Jasmine.

## Project layout

```
src/app/
  core/
    guards/         — route guards (auth required / guest-only)
    interceptors/    — HTTP interceptors (Bearer token attachment, 401 refresh-and-retry)
    models/          — TypeScript interfaces mirroring backend API DTOs
    pipes/           — the `translate` pipe for i18n
    services/        — one service per backend feature area (auth, subscriptions, budgets, workspace, security, catalog, reports)
  layout/shell/      — authenticated-app shell (sidenav, topbar, theme/locale toggles)
  features/          — one folder per page/feature (auth, dashboard, subscriptions, budgets, workspace, security, settings, reports)
  app.routes.ts      — route table (lazy-loaded, guard-protected)
```

## i18n

Translation dictionaries live in `public/i18n/en.json` and `public/i18n/ar.json` (served as static assets, not
compiled in). When adding UI text, add the key to **both** files and use the `translate` pipe
(`{{ 'some.key' | translate }}`) rather than hardcoding strings — otherwise Arabic/RTL support silently breaks for
that string.

## Code scaffolding

This project was generated with [Angular CLI](https://github.com/angular/angular-cli). Angular CLI's schematics
still work if you want to scaffold a new component/service in the established style:

```bash
ng generate component features/some-feature/some-feature
```
