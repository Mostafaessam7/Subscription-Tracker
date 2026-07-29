/** Mirrors SubscriptionTracker.Domain.Identity.Permissions on the backend - keep both in sync. */
export const Permissions = {
  Subscriptions: {
    View: 'subscriptions:view',
    Create: 'subscriptions:create',
    Edit: 'subscriptions:edit',
    Delete: 'subscriptions:delete',
    Cancel: 'subscriptions:cancel',
  },
  Workspace: {
    ManageMembers: 'workspace:manage-members',
    ManageSettings: 'workspace:manage-settings',
    ManageRoles: 'workspace:manage-roles',
  },
  Reports: {
    View: 'reports:view',
    Export: 'reports:export',
  },
  Budgets: {
    View: 'budgets:view',
    Manage: 'budgets:manage',
  },
  Catalog: {
    View: 'catalog:view',
    Manage: 'catalog:manage',
  },
} as const;
