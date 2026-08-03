export interface WorkspaceMember {
  memberId: string;
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roleId: string;
  roleName: string;
  status: string;
}

export interface Workspace {
  id: string;
  name: string;
  ownerId: string;
  defaultCurrencyCode: string;
  timeZoneId: string;
  locale: string;
  members: WorkspaceMember[];
}

export interface AssignableRole {
  id: string;
  name: string;
  description: string | null;
}

export interface PendingInvitation {
  workspaceId: string;
  workspaceName: string;
  memberId: string;
  roleName: string;
}

export interface UpdateWorkspaceSettingsRequest {
  defaultCurrencyCode: string;
  timeZoneId: string;
  locale: string;
}

export interface InviteMemberRequest {
  email: string;
  roleId: string;
}

export interface MyWorkspaceSummary {
  id: string;
  name: string;
  roleName: string;
  isOwner: boolean;
  isCurrent: boolean;
}
