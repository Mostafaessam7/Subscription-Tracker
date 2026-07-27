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
    TimeProvider timeProvider)
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
        if (!existingToken.IsActive)
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
