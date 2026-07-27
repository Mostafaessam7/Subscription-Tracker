using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Subscriptions.PauseSubscription;

public sealed record PauseSubscriptionCommand(Guid SubscriptionId) : ICommand;
