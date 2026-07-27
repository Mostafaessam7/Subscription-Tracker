import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { BehaviorSubject, catchError, filter, switchMap, take, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { TokenStorageService } from '../services/token-storage.service';

let isRefreshing = false;
const refreshedToken$ = new BehaviorSubject<string | null>(null);

const AUTH_FREE_PATHS = ['/auth/login', '/auth/register', '/auth/refresh-token'];

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const tokenStorage = inject(TokenStorageService);
  const authService = inject(AuthService);
  const router = inject(Router);

  const isAuthFreeRequest = AUTH_FREE_PATHS.some((path) => request.url.includes(path));
  const accessToken = tokenStorage.getAccessToken();

  const authorizedRequest = accessToken && !isAuthFreeRequest
    ? request.clone({ setHeaders: { Authorization: `Bearer ${accessToken}` } })
    : request;

  return next(authorizedRequest).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401 || isAuthFreeRequest) {
        return throwError(() => error);
      }

      if (!tokenStorage.getRefreshToken()) {
        authService.clearSession();
        router.navigateByUrl('/auth/login');
        return throwError(() => error);
      }

      if (!isRefreshing) {
        isRefreshing = true;
        refreshedToken$.next(null);

        return authService.refreshToken().pipe(
          switchMap((response) => {
            isRefreshing = false;
            refreshedToken$.next(response.accessToken);
            return next(request.clone({ setHeaders: { Authorization: `Bearer ${response.accessToken}` } }));
          }),
          catchError((refreshError: unknown) => {
            isRefreshing = false;
            authService.clearSession();
            router.navigateByUrl('/auth/login');
            return throwError(() => refreshError);
          }),
        );
      }

      return refreshedToken$.pipe(
        filter((token): token is string => token !== null),
        take(1),
        switchMap((token) => next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }))),
      );
    }),
  );
};
