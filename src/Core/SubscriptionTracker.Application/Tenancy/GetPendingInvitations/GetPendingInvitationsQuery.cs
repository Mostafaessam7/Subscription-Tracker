using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.GetPendingInvitations;

public sealed record GetPendingInvitationsQuery : IQuery<IReadOnlyList<PendingInvitationDto>>;
