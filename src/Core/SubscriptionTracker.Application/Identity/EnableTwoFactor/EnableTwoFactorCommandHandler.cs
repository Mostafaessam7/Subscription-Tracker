using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Identity.EnableTwoFactor;

public sealed class EnableTwoFactorCommandHandler(
    IRepository<User, Guid> userRepository, ICurrentUserService currentUserService, ITwoFactorService twoFactorService)
    : ICommandHandler<EnableTwoFactorCommand>
{
    public async Task<Result> Handle(EnableTwoFactorCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Failure(Error.Unauthorized("EnableTwoFactor.NotSignedIn", "You must be signed in."));
        }

        var user = await userRepository.GetByIdAsync(currentUserService.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("EnableTwoFactor.NotFound", "User was not found."));
        }

        if (!twoFactorService.ValidateCode(request.Secret, request.Code))
        {
            return Result.Failure(Error.Validation("EnableTwoFactor.InvalidCode", "The verification code is incorrect."));
        }

        user.EnableTwoFactor(request.Secret);
        userRepository.Update(user);

        return Result.Success();
    }
}
