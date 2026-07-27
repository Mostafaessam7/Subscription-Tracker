namespace SubscriptionTracker.Domain.Identity;

/// <summary>Catalog of permission codes usable in permission-based authorization policies.</summary>
public static class Permissions
{
    public static class Subscriptions
    {
        public const string View = "subscriptions:view";
        public const string Create = "subscriptions:create";
        public const string Edit = "subscriptions:edit";
        public const string Delete = "subscriptions:delete";
        public const string Cancel = "subscriptions:cancel";
    }

    public static class Workspace
    {
        public const string ManageMembers = "workspace:manage-members";
        public const string ManageSettings = "workspace:manage-settings";
        public const string ManageRoles = "workspace:manage-roles";
    }

    public static class Reports
    {
        public const string View = "reports:view";
        public const string Export = "reports:export";
    }

    public static class Budgets
    {
        public const string View = "budgets:view";
        public const string Manage = "budgets:manage";
    }

    public static class Catalog
    {
        public const string View = "catalog:view";
        public const string Manage = "catalog:manage";
    }

    public static IReadOnlyCollection<string> All { get; } =
    [
        Subscriptions.View, Subscriptions.Create, Subscriptions.Edit, Subscriptions.Delete, Subscriptions.Cancel,
        Workspace.ManageMembers, Workspace.ManageSettings, Workspace.ManageRoles,
        Reports.View, Reports.Export,
        Budgets.View, Budgets.Manage,
        Catalog.View, Catalog.Manage,
    ];
}
