using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.ChangeMemberRole;

public sealed record ChangeMemberRoleCommand(Guid MemberId, Guid RoleId) : ICommand;
