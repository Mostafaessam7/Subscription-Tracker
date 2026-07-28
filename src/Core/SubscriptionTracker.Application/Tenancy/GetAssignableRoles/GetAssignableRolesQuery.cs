using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.GetAssignableRoles;

/// <summary>Returns the global system role templates (Member/Viewer) a workspace can assign when inviting a member.
/// Deliberately excludes each workspace's ad-hoc "Owner" role - there's exactly one Owner per workspace, assigned
/// at registration, and no custom-role-builder exists yet (see HANDOVER.md).</summary>
public sealed record GetAssignableRolesQuery : IQuery<IReadOnlyList<AssignableRoleDto>>;
