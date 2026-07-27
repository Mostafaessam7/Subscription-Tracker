using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Subscriptions.GetSubscriptionById;

public sealed record GetSubscriptionByIdQuery(Guid SubscriptionId) : IQuery<SubscriptionDto>;
