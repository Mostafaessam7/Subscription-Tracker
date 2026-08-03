import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AdminUserSummary, AdminWorkspaceSummary, SystemHealth } from '../models/admin.models';

@Injectable({ providedIn: 'root' })
export class AdminService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/admin`;

  getWorkspaces(): Observable<AdminWorkspaceSummary[]> {
    return this.http.get<AdminWorkspaceSummary[]>(`${this.baseUrl}/workspaces`);
  }

  getUsers(): Observable<AdminUserSummary[]> {
    return this.http.get<AdminUserSummary[]>(`${this.baseUrl}/users`);
  }

  getSystemHealth(): Observable<SystemHealth> {
    return this.http.get<SystemHealth>(`${this.baseUrl}/health`);
  }

  disableUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/${id}/disable`, {});
  }

  enableUser(id: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/users/${id}/enable`, {});
  }
}
