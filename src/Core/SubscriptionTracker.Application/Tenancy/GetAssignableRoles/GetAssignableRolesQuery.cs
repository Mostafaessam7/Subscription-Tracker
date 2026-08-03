using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.GetAssignableRoles;

/// <summary>Returns every role a workspace can assign when inviting a member: the global system role templates
/// (Member/Viewer) plus this workspace's own roles - its ad-hoc "Owner" role and any custom roles created via
/// the role builder (see Tenancy/Roles/CreateRole). A workspace admin assigning "Owner" to another member is a
/// legitimate use of the role builder, not a special case to guard against.</summary>
public sealed record GetAssignableRolesQuery : IQuery<IReadOnlyList<AssignableRoleDto>>;
