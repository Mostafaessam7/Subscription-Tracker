using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.RefreshToken;

public sealed record RefreshTokenCommand(string RefreshToken, Guid? WorkspaceId, string? IpAddress) : ICommand<RefreshTokenResponse>;

public sealed record RefreshTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken);
