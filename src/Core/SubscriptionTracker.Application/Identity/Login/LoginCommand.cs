using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.Login;

public sealed record LoginCommand(string Email, string Password, string? IpAddress) : ICommand<LoginResponse>;

public sealed record LoginResponse(
    Guid UserId, string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken, Guid? WorkspaceId);
