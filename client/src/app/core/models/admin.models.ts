export interface AdminWorkspaceSummary {
  id: string;
  name: string;
  ownerEmail: string;
  memberCount: number;
  createdAtUtc: string;
}

export interface AdminUserSummary {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  status: string;
  isSystemAdmin: boolean;
  isEmailVerified: boolean;
  createdAtUtc: string;
}

export interface SystemHealth {
  totalUsers: number;
  totalWorkspaces: number;
  totalSubscriptions: number;
  activeSubscriptions: number;
  totalBudgets: number;
}
