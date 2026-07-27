using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : ICommand;
