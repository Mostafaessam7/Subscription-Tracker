using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.Logout;

public sealed record LogoutCommand(string RefreshToken) : ICommand;
