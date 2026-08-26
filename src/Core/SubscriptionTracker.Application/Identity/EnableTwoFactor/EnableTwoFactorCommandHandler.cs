using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Identity.EnableTwoFactor;

public sealed class EnableTwoFactorCommandHandler(
    IRepository<User, Guid> userRepository,
    ICurrentUserService currentUserService,
    ITwoFactorService twoFactorService,
    IPasswordHasher passwordHasher)
    : ICommandHandler<EnableTwoFactorCommand, EnableTwoFactorResponse>
{
    private const int RecoveryCodeCount = 10;

    public async Task<Result<EnableTwoFactorResponse>> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Failure<EnableTwoFactorResponse>(Error.Unauthorized("EnableTwoFactor.NotSignedIn", "You must be signed in."));
        }

        var user = await userRepository.GetByIdAsync(currentUserService.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<EnableTwoFactorResponse>(Error.NotFound("EnableTwoFactor.NotFound", "User was not found."));
        }

        if (!twoFactorService.ValidateCode(request.Secret, request.Code))
        {
            return Result.Failure<EnableTwoFactorResponse>(Error.Validation("EnableTwoFactor.InvalidCode", "The verification code is incorrect."));
        }

        user.EnableTwoFactor(request.Secret);

        // Recovery codes are the only way back into the account if the authenticator device is lost - hashed
        // with the same PBKDF2 IPasswordHasher used for account passwords, never persisted in the clear.
        var rawRecoveryCodes = twoFactorService.GenerateRecoveryCodes(RecoveryCodeCount);
        user.ReplaceRecoveryCodes(rawRecoveryCodes.Select(passwordHasher.Hash));

        userRepository.Update(user);

        return Result.Success(new EnableTwoFactorResponse(rawRecoveryCodes));
    }
}
