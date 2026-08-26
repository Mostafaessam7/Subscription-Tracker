using Microsoft.Extensions.Logging;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Identity.RefreshToken;

public sealed class RefreshTokenCommandHandler(
    IRepository<User, Guid> userRepository,
    IRepository<Workspace, Guid> workspaceRepository,
    IRepository<Role, Guid> roleRepository,
    IJwtTokenService jwtTokenService,
    TimeProvider timeProvider,
    ILogger<RefreshTokenCommandHandler> logger)
    : ICommandHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly Error InvalidTokenError = Error.Unauthorized("RefreshToken.Invalid", "The refresh token is invalid or has expired.");

    public async Task<Result<RefreshTokenResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);

        var user = await userRepository.FirstOrDefaultAsync(new UserByRefreshTokenHashSpecification(tokenHash), cancellationToken);
        if (user is null)
        {
            return Result.Failure<RefreshTokenResponse>(InvalidTokenError);
        }

        var existingToken = user.RefreshTokens.First(t => t.TokenHash == tokenHash);

        // Reuse of an already-revoked token is a stronger signal than "just expired": every refresh rotates
        // the token (revokes the old one, issues a new one), so the only way a *revoked* token gets presented
        // again is either a client retrying a stale value it should have discarded, or someone else replaying
        // a token they captured before its legitimate rotation. Treat it as a possible theft and burn the
        // whole session family - the OWASP-recommended response for rotation-based refresh tokens - rather
        // than silently rejecting just this one request and leaving every other still-active token (including
        // one an attacker may already hold) untouched.
        if (existingToken.IsRevoked)
        {
            logger.LogWarning(
                "Revoked refresh token replayed for user {UserId} from {IpAddress} - revoking all active refresh tokens for this account as a precaution.",
                user.Id, request.IpAddress);
            user.RevokeAllRefreshTokens(request.IpAddress);
            userRepository.Update(user);
            return Result.Failure<RefreshTokenResponse>(InvalidTokenError);
        }

        if (existingToken.IsExpired)
        {
            return Result.Failure<RefreshTokenResponse>(InvalidTokenError);
        }

        var now = timeProvider.GetUtcNow();
        var newRawRefreshToken = jwtTokenService.GenerateRefreshToken();
        var newTokenHash = jwtTokenService.HashRefreshToken(newRawRefreshToken);

        user.RevokeRefreshToken(tokenHash, request.IpAddress);
        user.IssueRefreshToken(newTokenHash, now.Add(RefreshTokenLifetime), request.IpAddress);

        Workspace? workspace = null;
        if (request.WorkspaceId is not null)
        {
            workspace = await workspaceRepository.GetByIdAsync(request.WorkspaceId.Value, cancellationToken);
            if (workspace is null || workspace.Members.All(m => m.UserId != user.Id || m.Status != WorkspaceMemberStatus.Active))
            {
                return Result.Failure<RefreshTokenResponse>(
                    Error.Forbidden("RefreshToken.NotAWorkspaceMember", "You are not an active member of this workspace."));
            }
        }

        var permissionCodes = Array.Empty<string>();
        if (workspace is not null)
        {
            var member = workspace.Members.First(m => m.UserId == user.Id);
            var role = await roleRepository.GetByIdAsync(member.RoleId, cancellationToken);
            permissionCodes = role?.PermissionCodes.ToArray() ?? [];
        }

        var accessToken = jwtTokenService.GenerateAccessToken(user, workspace?.Id, permissionCodes);

        userRepository.Update(user);

        return new RefreshTokenResponse(accessToken.Token, accessToken.ExpiresAtUtc, newRawRefreshToken);
    }
}
