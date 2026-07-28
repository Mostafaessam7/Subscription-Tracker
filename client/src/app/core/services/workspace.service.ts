import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  AssignableRole,
  InviteMemberRequest,
  PendingInvitation,
  UpdateWorkspaceSettingsRequest,
  Workspace,
} from '../models/workspace.models';

@Injectable({ providedIn: 'root' })
export class WorkspaceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/workspace`;

  getMyWorkspace(): Observable<Workspace> {
    return this.http.get<Workspace>(this.baseUrl);
  }

  getAssignableRoles(): Observable<AssignableRole[]> {
    return this.http.get<AssignableRole[]>(`${this.baseUrl}/assignable-roles`);
  }

  getPendingInvitations(): Observable<PendingInvitation[]> {
    return this.http.get<PendingInvitation[]>(`${this.baseUrl}/invitations`);
  }

  updateSettings(request: UpdateWorkspaceSettingsRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/settings`, request);
  }

  inviteMember(request: InviteMemberRequest): Observable<string> {
    return this.http.post<string>(`${this.baseUrl}/members`, request);
  }

  acceptInvitation(memberId: string): Observable<void> {
    return this.http.post<void>(`${this.baseUrl}/members/${memberId}/accept`, {});
  }

  changeMemberRole(memberId: string, roleId: string): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/members/${memberId}/role`, { roleId });
  }

  removeMember(memberId: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/members/${memberId}`);
  }
}
