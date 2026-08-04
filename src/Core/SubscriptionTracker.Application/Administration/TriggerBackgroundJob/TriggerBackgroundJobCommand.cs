using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Administration.TriggerBackgroundJob;

public sealed record TriggerBackgroundJobCommand(string JobName) : ICommand;
