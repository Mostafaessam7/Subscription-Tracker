using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.ForgotPassword;

public sealed record ForgotPasswordCommand(string Email) : ICommand;
