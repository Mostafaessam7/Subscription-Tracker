using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.AcceptInvitation;

public sealed record AcceptInvitationCommand(Guid MemberId) : ICommand;
