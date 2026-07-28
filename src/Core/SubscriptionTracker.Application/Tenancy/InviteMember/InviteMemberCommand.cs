using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.InviteMember;

public sealed record InviteMemberCommand(string Email, Guid RoleId) : ICommand<Guid>;
