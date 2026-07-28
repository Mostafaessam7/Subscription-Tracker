using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.DisableTwoFactor;

public sealed record DisableTwoFactorCommand(string Code) : ICommand;
