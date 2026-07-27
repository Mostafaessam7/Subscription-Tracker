using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Domain.Tenancy.Specifications;

namespace SubscriptionTracker.Application.Identity.Login;

public sealed class LoginCommandHandler(
    IRepository<User, Guid> userRepository,
    IRepository<Workspace, Guid> workspaceRepository,
    IRepository<Role, Guid> roleRepository,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService,
    TimeProvider timeProvider)
    : ICommandHandler<LoginCommand, LoginResponse>
{
    private static readonly TimeSpan RefreshTokenLifetime = TimeSpan.FromDays(30);
    private static readonly Error InvalidCredentialsError = Error.Unauthorized("Login.InvalidCredentials", "Invalid email or password.");

    public async Task<Result<LoginResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<LoginResponse>(InvalidCredentialsError);
        }

        var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken);
        if (user is null)
        {
            return Result.Failure<LoginResponse>(InvalidCredentialsError);
        }

        var now = timeProvider.GetUtcNow();

        if (user.IsLockedOut)
        {
            return Result.Failure<LoginResponse>(
                Error.Unauthorized("Login.AccountLocked", "This account is temporarily locked due to too many failed attempts."));
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            user.RecordFailedLogin(now);
            userRepository.Update(user);
            return Result.Failure<LoginResponse>(InvalidCredentialsError);
        }

        if (user.Status is UserStatus.Disabled)
        {
            return Result.Failure<LoginResponse>(Error.Unauthorized("Login.AccountDisabled", "This account has been disabled."));
        }

        user.RecordSuccessfulLogin(now);

        var workspaces = await workspaceRepository.ListAsync(new WorkspacesByMemberUserIdSpecification(user.Id), cancellationToken);
        var primaryWorkspace = workspaces.FirstOrDefault(w => w.OwnerId == user.Id) ?? (workspaces.Count > 0 ? workspaces[0] : null);

        var permissionCodes = Array.Empty<string>();
        if (primaryWorkspace is not null)
        {
            var member = primaryWorkspace.Members.First(m => m.UserId == user.Id);
            var role = await roleRepository.GetByIdAsync(member.RoleId, cancellationToken);
            permissionCodes = role?.PermissionCodes.ToArray() ?? [];
        }

        var accessToken = jwtTokenService.GenerateAccessToken(user, primaryWorkspace?.Id, permissionCodes);
        var rawRefreshToken = jwtTokenService.GenerateRefreshToken();
        user.IssueRefreshToken(jwtTokenService.HashRefreshToken(rawRefreshToken), now.Add(RefreshTokenLifetime), request.IpAddress);

        userRepository.Update(user);

        return new LoginResponse(user.Id, accessToken.Token, accessToken.ExpiresAtUtc, rawRefreshToken, primaryWorkspace?.Id);
    }
}
