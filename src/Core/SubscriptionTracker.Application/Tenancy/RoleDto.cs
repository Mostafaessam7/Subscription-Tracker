namespace SubscriptionTracker.Application.Tenancy;

public sealed record RoleDetailDto(
    Guid Id, string Name, string? Description, bool IsSystemRole, IReadOnlyCollection<string> Permissions);

public sealed record PermissionCatalogEntryDto(string Code, string Category);
