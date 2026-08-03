export interface RoleDetail {
  id: string;
  name: string;
  description: string | null;
  isSystemRole: boolean;
  permissions: string[];
}

export interface PermissionCatalogEntry {
  code: string;
  category: string;
}

export interface CreateRoleRequest {
  name: string;
  description: string | null;
  permissionCodes: string[];
}

export interface UpdateRoleRequest {
  name: string;
  description: string | null;
  permissionCodes: string[];
}
