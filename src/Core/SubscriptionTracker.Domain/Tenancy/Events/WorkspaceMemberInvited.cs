using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Tenancy.Events;

public sealed record WorkspaceMemberInvited(Guid WorkspaceId, Guid MemberId, Guid UserId, Guid RoleId) : DomainEvent;
