import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  LoginRequest,
  LoginResponse,
  RefreshTokenResponse,
  RegisterRequest,
  RegisterResponse,
} from '../models/auth.models';
import { TokenStorageService } from './token-storage.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly tokenStorage = inject(TokenStorageService);

  private readonly isAuthenticatedSignal = signal<boolean>(this.tokenStorage.getAccessToken() !== null);

  readonly isAuthenticated = computed(() => this.isAuthenticatedSignal());

  register(request: RegisterRequest): Observable<RegisterResponse> {
    return this.http.post<RegisterResponse>(`${environment.apiBaseUrl}/auth/register`, request);
  }

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http.post<LoginResponse>(`${environment.apiBaseUrl}/auth/login`, request).pipe(
      tap((response) => {
        this.tokenStorage.save({
          accessToken: response.accessToken,
          accessTokenExpiresAtUtc: response.accessTokenExpiresAtUtc,
          refreshToken: response.refreshToken,
          workspaceId: response.workspaceId,
          userId: response.userId,
        });
        this.isAuthenticatedSignal.set(true);
      }),
    );
  }

  refreshToken(): Observable<RefreshTokenResponse> {
    const refreshToken = this.tokenStorage.getRefreshToken();
    const workspaceId = this.tokenStorage.getWorkspaceId();

    return this.http
      .post<RefreshTokenResponse>(`${environment.apiBaseUrl}/auth/refresh-token`, { refreshToken, workspaceId })
      .pipe(
        tap((response) => {
          this.tokenStorage.updateTokens(response.accessToken, response.accessTokenExpiresAtUtc, response.refreshToken);
        }),
      );
  }

  logout(): Observable<void> {
    const refreshToken = this.tokenStorage.getRefreshToken();

    return this.http.post<void>(`${environment.apiBaseUrl}/auth/logout`, { refreshToken }).pipe(
      tap(() => this.clearSession()),
    );
  }

  clearSession(): void {
    this.tokenStorage.clear();
    this.isAuthenticatedSignal.set(false);
  }
}
