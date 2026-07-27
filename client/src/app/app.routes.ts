import { Routes } from '@angular/router';
import { authGuard, guestGuard } from './core/guards/auth.guard';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  {
    path: 'auth',
    canActivate: [guestGuard],
    children: [
      { path: 'login', loadComponent: () => import('./features/auth/login/login').then((m) => m.Login) },
      { path: 'register', loadComponent: () => import('./features/auth/register/register').then((m) => m.Register) },
      {
        path: 'verify-email',
        loadComponent: () => import('./features/auth/verify-email/verify-email').then((m) => m.VerifyEmail),
      },
      {
        path: 'forgot-password',
        loadComponent: () =>
          import('./features/auth/forgot-password/forgot-password').then((m) => m.ForgotPassword),
      },
      {
        path: 'reset-password',
        loadComponent: () =>
          import('./features/auth/reset-password/reset-password').then((m) => m.ResetPassword),
      },
      { path: '', pathMatch: 'full', redirectTo: 'login' },
    ],
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./layout/shell/shell').then((m) => m.Shell),
    children: [
      { path: 'dashboard', loadComponent: () => import('./features/dashboard/dashboard').then((m) => m.Dashboard) },
      { path: 'settings', loadComponent: () => import('./features/settings/settings').then((m) => m.Settings) },
      {
        path: 'subscriptions',
        children: [
          {
            path: '',
            pathMatch: 'full',
            loadComponent: () =>
              import('./features/subscriptions/subscription-list/subscription-list').then((m) => m.SubscriptionList),
          },
          {
            path: 'new',
            loadComponent: () =>
              import('./features/subscriptions/subscription-form/subscription-form').then((m) => m.SubscriptionForm),
          },
          {
            path: ':id/edit',
            loadComponent: () =>
              import('./features/subscriptions/subscription-form/subscription-form').then((m) => m.SubscriptionForm),
          },
          {
            path: ':id',
            loadComponent: () =>
              import('./features/subscriptions/subscription-detail/subscription-detail').then(
                (m) => m.SubscriptionDetail,
              ),
          },
        ],
      },
    ],
  },
  { path: '**', redirectTo: 'dashboard' },
];
