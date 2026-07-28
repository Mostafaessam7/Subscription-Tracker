using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.RemoveMember;

public sealed record RemoveMemberCommand(Guid MemberId) : ICommand;
