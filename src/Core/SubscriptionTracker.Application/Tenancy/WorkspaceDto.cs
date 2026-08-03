namespace SubscriptionTracker.Application.Tenancy;

public sealed record WorkspaceDto(
    Guid Id,
    string Name,
    Guid OwnerId,
    string DefaultCurrencyCode,
    string TimeZoneId,
    string Locale,
    IReadOnlyCollection<WorkspaceMemberDto> Members);

public sealed record WorkspaceMemberDto(
    Guid MemberId,
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    Guid RoleId,
    string RoleName,
    string Status);

public sealed record PendingInvitationDto(Guid WorkspaceId, string WorkspaceName, Guid MemberId, string RoleName);

public sealed record AssignableRoleDto(Guid Id, string Name, string? Description);

public sealed record MyWorkspaceSummaryDto(Guid Id, string Name, string RoleName, bool IsOwner, bool IsCurrent);
