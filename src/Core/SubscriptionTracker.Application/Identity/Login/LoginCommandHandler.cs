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
    ITwoFactorService twoFactorService,
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

        if (user.TwoFactorEnabled)
        {
            if (string.IsNullOrWhiteSpace(request.TotpCode))
            {
                // Password was correct but no code was supplied yet - the frontend should prompt for one and
                // retry the same login call. Not a failed attempt: nothing wrong has been guessed yet.
                return Result.Failure<LoginResponse>(
                    Error.Validation("Login.TwoFactorRequired", "A verification code is required to complete sign-in."));
            }

            // A TOTP code is always 6 digits; anything else (e.g. "WXYZ2-3456") can only be a recovery code,
            // so only pay the PBKDF2-per-candidate cost of TryConsumeRecoveryCode on that path, not on every
            // ordinary TOTP login.
            var isValidTotpCode = twoFactorService.ValidateCode(user.TwoFactorSecret!, request.TotpCode);
            var isValidRecoveryCode = !isValidTotpCode && TryConsumeRecoveryCode(user, request.TotpCode);

            if (!isValidTotpCode && !isValidRecoveryCode)
            {
                user.RecordFailedLogin(now);
                userRepository.Update(user);
                return Result.Failure<LoginResponse>(
                    Error.Unauthorized("Login.InvalidTwoFactorCode", "The verification code is incorrect."));
            }
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

    /// <summary>
    /// Checks <paramref name="candidate"/> against every still-unused recovery code hash and consumes the
    /// first match. Recovery codes are single-use by design (unlike a TOTP code, which is naturally one-time
    /// via the moving time step) - a match must be spent immediately or it could be replayed.
    /// </summary>
    private bool TryConsumeRecoveryCode(User user, string candidate)
    {
        foreach (var recoveryCode in user.RecoveryCodes.Where(c => !c.IsUsed))
        {
            if (passwordHasher.Verify(candidate, recoveryCode.CodeHash))
            {
                return user.ConsumeRecoveryCode(recoveryCode.Id).IsSuccess;
            }
        }

        return false;
    }
}
