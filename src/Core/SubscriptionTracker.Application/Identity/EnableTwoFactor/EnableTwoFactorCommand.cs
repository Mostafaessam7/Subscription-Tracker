using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.EnableTwoFactor;

public sealed record EnableTwoFactorCommand(string Secret, string Code) : ICommand;
