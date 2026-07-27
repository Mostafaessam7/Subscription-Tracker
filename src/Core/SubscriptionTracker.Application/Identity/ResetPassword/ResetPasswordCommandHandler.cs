using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Security;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Identity.ResetPassword;

public sealed class ResetPasswordCommandHandler(IRepository<User, Guid> userRepository, IPasswordHasher passwordHasher)
    : ICommandHandler<ResetPasswordCommand>
{
    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("ResetPassword.UserNotFound", "User was not found."));
        }

        var tokenHash = SecureTokenGenerator.Hash(request.Token);
        var consumeResult = user.ConsumeVerificationToken(tokenHash, VerificationTokenPurpose.PasswordReset);
        if (consumeResult.IsFailure)
        {
            return Result.Failure(consumeResult.Error);
        }

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);
        var changeResult = user.ChangePassword(newPasswordHash);
        if (changeResult.IsFailure)
        {
            return changeResult;
        }

        user.RevokeAllRefreshTokens(revokedByIp: null);
        user.Unlock();
        userRepository.Update(user);

        return Result.Success();
    }
}
