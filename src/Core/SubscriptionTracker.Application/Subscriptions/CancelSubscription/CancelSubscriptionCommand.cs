using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Subscriptions.CancelSubscription;

public sealed record CancelSubscriptionCommand(Guid SubscriptionId, DateOnly EffectiveDate, string? Reason) : ICommand;
