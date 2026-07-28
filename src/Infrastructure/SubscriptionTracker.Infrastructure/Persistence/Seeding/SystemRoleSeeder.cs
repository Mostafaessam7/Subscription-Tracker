using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Infrastructure.Persistence.Seeding;

/// <summary>
/// Seeds the two global (WorkspaceId = null) template roles that workspace owners can assign when inviting a
/// member, so every workspace isn't stuck offering only its own ad-hoc "Owner" role (see RegisterUserCommandHandler,
/// which still creates that per-workspace Owner role separately - these seeded roles are additive, not a replacement).
/// Idempotent: checks by name+IsSystemRole before inserting, safe to run on every startup.
/// </summary>
public static class SystemRoleSeeder
{
    public const string MemberRoleName = "Member";
    public const string ViewerRoleName = "Viewer";

    private static readonly string[] MemberPermissions =
    [
        Permissions.Subscriptions.View,
        Permissions.Subscriptions.Create,
        Permissions.Subscriptions.Edit,
        Permissions.Subscriptions.Cancel,
        Permissions.Catalog.View,
        Permissions.Catalog.Manage,
        Permissions.Budgets.View,
        Permissions.Reports.View,
        Permissions.Reports.Export,
    ];

    private static readonly string[] ViewerPermissions =
    [
        Permissions.Subscriptions.View,
        Permissions.Catalog.View,
        Permissions.Budgets.View,
        Permissions.Reports.View,
    ];

    public static async Task SeedAsync(ApplicationDbContext dbContext, CancellationToken cancellationToken = default)
    {
        var existingSystemRoleNames = await dbContext.Roles
            .Where(r => r.IsSystemRole)
            .Select(r => r.Name)
            .ToListAsync(cancellationToken);

        if (!existingSystemRoleNames.Contains(MemberRoleName))
        {
            dbContext.Roles.Add(CreateSystemRole(MemberRoleName, "Can manage subscriptions and view budgets/reports", MemberPermissions));
        }

        if (!existingSystemRoleNames.Contains(ViewerRoleName))
        {
            dbContext.Roles.Add(CreateSystemRole(ViewerRoleName, "Read-only access to subscriptions, budgets, and reports", ViewerPermissions));
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static Role CreateSystemRole(string name, string description, IEnumerable<string> permissions)
    {
        var role = Role.Create(name, description, workspaceId: null, isSystemRole: true).Value;
        foreach (var permission in permissions)
        {
            role.GrantPermission(permission);
        }

        return role;
    }
}
