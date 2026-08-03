using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Administration.DisableUser;

public sealed record DisableUserCommand(Guid UserId) : ICommand;
