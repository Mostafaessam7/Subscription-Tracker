using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.Roles.GetWorkspaceRoles;

/// <summary>Every role assignable within the current workspace: global system templates plus this workspace's
/// own roles (the ad-hoc Owner role and any custom roles created via the role builder).</summary>
public sealed record GetWorkspaceRolesQuery : IQuery<IReadOnlyList<RoleDetailDto>>;
