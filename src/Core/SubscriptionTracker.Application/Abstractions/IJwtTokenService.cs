using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Abstractions;

public sealed record AccessTokenResult(string Token, DateTimeOffset ExpiresAtUtc);

public interface IJwtTokenService
{
    AccessTokenResult GenerateAccessToken(User user, Guid? workspaceId, IReadOnlyCollection<string> permissionCodes);

    string GenerateRefreshToken();

    string HashRefreshToken(string refreshToken);
}
