using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity;

public sealed class Role : AuditableAggregateRoot<Guid>
{
    // EF Core's primitive-collection value comparer requires an ordered IList<T>, so this is a List
    // with manual deduplication rather than a HashSet, even though it is semantically a set.
    private readonly List<string> _permissions = [];

    private Role(Guid id, Guid? workspaceId, string name, string? description, bool isSystemRole)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Name = name;
        Description = description;
        IsSystemRole = isSystemRole;
    }

    private Role()
    {
    }

    /// <summary>Null for global/system roles shared across all workspaces.</summary>
    public Guid? WorkspaceId { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystemRole { get; private set; }

    public IReadOnlyCollection<string> PermissionCodes => _permissions.ToList().AsReadOnly();

    public static Result<Role> Create(string name, string? description, Guid? workspaceId, bool isSystemRole = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure<Role>(Error.Validation("Role.EmptyName", "Role name cannot be empty."));
        }

        return new Role(Guid.NewGuid(), workspaceId, name.Trim(), description?.Trim(), isSystemRole);
    }

    public Result Rename(string name)
    {
        if (IsSystemRole)
        {
            return Result.Failure(Error.Failure("Role.SystemRoleImmutable", "System roles cannot be renamed."));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Result.Failure(Error.Validation("Role.EmptyName", "Role name cannot be empty."));
        }

        Name = name.Trim();
        return Result.Success();
    }

    public Result UpdateDetails(string name, string? description)
    {
        var renameResult = Rename(name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        Description = description?.Trim();
        return Result.Success();
    }

    public Result GrantPermission(string permissionCode)
    {
        if (!Permissions.All.Contains(permissionCode))
        {
            return Result.Failure(Error.Validation("Role.UnknownPermission", $"Unknown permission code '{permissionCode}'."));
        }

        if (!_permissions.Contains(permissionCode))
        {
            _permissions.Add(permissionCode);
        }

        return Result.Success();
    }

    public void RevokePermission(string permissionCode) => _permissions.Remove(permissionCode);

    public bool HasPermission(string permissionCode) => _permissions.Contains(permissionCode);
}
