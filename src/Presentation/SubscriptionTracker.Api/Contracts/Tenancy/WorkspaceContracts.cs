namespace SubscriptionTracker.Api.Contracts.Tenancy;

public sealed record UpdateWorkspaceSettingsRequest(string DefaultCurrencyCode, string TimeZoneId, string Locale);

public sealed record InviteMemberRequest(string Email, Guid RoleId);

public sealed record ChangeMemberRoleRequest(Guid RoleId);

public sealed record CreateRoleRequest(string Name, string? Description, IReadOnlyCollection<string> PermissionCodes);

public sealed record UpdateRoleRequest(string Name, string? Description, IReadOnlyCollection<string> PermissionCodes);
