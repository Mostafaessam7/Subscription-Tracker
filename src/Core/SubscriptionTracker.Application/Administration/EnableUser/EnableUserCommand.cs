using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Administration.EnableUser;

public sealed record EnableUserCommand(Guid UserId) : ICommand;
