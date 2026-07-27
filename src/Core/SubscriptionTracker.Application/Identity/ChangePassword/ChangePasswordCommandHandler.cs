using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Identity.ChangePassword;

public sealed class ChangePasswordCommandHandler(
    IRepository<User, Guid> userRepository, ICurrentUserService currentUserService, IPasswordHasher passwordHasher)
    : ICommandHandler<ChangePasswordCommand>
{
    public async Task<Result> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Failure(Error.Unauthorized("ChangePassword.NotAuthenticated", "You must be signed in to change your password."));
        }

        var user = await userRepository.GetByIdAsync(currentUserService.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("ChangePassword.UserNotFound", "User was not found."));
        }

        if (!passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return Result.Failure(Error.Validation("ChangePassword.IncorrectCurrentPassword", "The current password is incorrect."));
        }

        var newPasswordHash = passwordHasher.Hash(request.NewPassword);
        var changeResult = user.ChangePassword(newPasswordHash);
        if (changeResult.IsFailure)
        {
            return changeResult;
        }

        user.RevokeAllRefreshTokens(revokedByIp: null);
        userRepository.Update(user);

        return Result.Success();
    }
}
