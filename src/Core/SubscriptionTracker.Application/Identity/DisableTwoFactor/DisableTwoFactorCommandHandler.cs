using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Identity.DisableTwoFactor;

public sealed class DisableTwoFactorCommandHandler(
    IRepository<User, Guid> userRepository, ICurrentUserService currentUserService, ITwoFactorService twoFactorService)
    : ICommandHandler<DisableTwoFactorCommand>
{
    public async Task<Result> Handle(DisableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Failure(Error.Unauthorized("DisableTwoFactor.NotSignedIn", "You must be signed in."));
        }

        var user = await userRepository.GetByIdAsync(currentUserService.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("DisableTwoFactor.NotFound", "User was not found."));
        }

        if (!user.TwoFactorEnabled || user.TwoFactorSecret is null)
        {
            return Result.Failure(Error.Validation("DisableTwoFactor.NotEnabled", "Two-factor authentication is not enabled."));
        }

        if (!twoFactorService.ValidateCode(user.TwoFactorSecret, request.Code))
        {
            return Result.Failure(Error.Validation("DisableTwoFactor.InvalidCode", "The verification code is incorrect."));
        }

        user.DisableTwoFactor();
        userRepository.Update(user);

        return Result.Success();
    }
}
