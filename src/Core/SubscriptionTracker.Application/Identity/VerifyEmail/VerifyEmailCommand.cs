using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.VerifyEmail;

public sealed record VerifyEmailCommand(Guid UserId, string Token) : ICommand;
