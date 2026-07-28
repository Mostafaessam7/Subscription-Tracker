import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CurrentUser, Session, SetupTwoFactorResponse } from '../models/security.models';

@Injectable({ providedIn: 'root' })
export class SecurityService {
  private readonly http = inject(HttpClient);

  getCurrentUser(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${environment.apiBaseUrl}/auth/me`);
  }

  getSessions(): Observable<Session[]> {
    return this.http.get<Session[]>(`${environment.apiBaseUrl}/sessions`);
  }

  revokeSession(id: string): Observable<void> {
    return this.http.delete<void>(`${environment.apiBaseUrl}/sessions/${id}`);
  }

  setupTwoFactor(): Observable<SetupTwoFactorResponse> {
    return this.http.get<SetupTwoFactorResponse>(`${environment.apiBaseUrl}/auth/2fa/setup`);
  }

  enableTwoFactor(secret: string, code: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/2fa/enable`, { secret, code });
  }

  disableTwoFactor(code: string): Observable<void> {
    return this.http.post<void>(`${environment.apiBaseUrl}/auth/2fa/disable`, { code });
  }
}
