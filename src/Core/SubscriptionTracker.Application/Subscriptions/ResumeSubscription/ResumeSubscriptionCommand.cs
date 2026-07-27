using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Subscriptions.ResumeSubscription;

public sealed record ResumeSubscriptionCommand(Guid SubscriptionId) : ICommand;
