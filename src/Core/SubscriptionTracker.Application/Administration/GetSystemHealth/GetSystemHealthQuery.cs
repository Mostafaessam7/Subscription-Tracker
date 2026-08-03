using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Administration.GetSystemHealth;

public sealed record GetSystemHealthQuery : IQuery<SystemHealthDto>;
