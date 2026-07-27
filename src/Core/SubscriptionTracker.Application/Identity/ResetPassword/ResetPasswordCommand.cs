using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.ResetPassword;

public sealed record ResetPasswordCommand(Guid UserId, string Token, string NewPassword) : ICommand;
