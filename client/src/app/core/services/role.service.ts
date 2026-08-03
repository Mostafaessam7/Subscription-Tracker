import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateRoleRequest, PermissionCatalogEntry, RoleDetail, UpdateRoleRequest } from '../models/role.models';

@Injectable({ providedIn: 'root' })
export class RoleService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/roles`;

  getWorkspaceRoles(): Observable<RoleDetail[]> {
    return this.http.get<RoleDetail[]>(this.baseUrl);
  }

  getPermissionCatalog(): Observable<PermissionCatalogEntry[]> {
    return this.http.get<PermissionCatalogEntry[]>(`${this.baseUrl}/permissions`);
  }

  createRole(request: CreateRoleRequest): Observable<string> {
    return this.http.post<string>(this.baseUrl, request);
  }

  updateRole(id: string, request: UpdateRoleRequest): Observable<void> {
    return this.http.put<void>(`${this.baseUrl}/${id}`, request);
  }

  deleteRole(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }
}
