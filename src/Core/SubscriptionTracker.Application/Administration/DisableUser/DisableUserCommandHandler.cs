using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Administration.DisableUser;

public sealed class DisableUserCommandHandler(IRepository<User, Guid> userRepository, ICurrentUserService currentUserService)
    : ICommandHandler<DisableUserCommand>
{
    public async Task<Result> Handle(DisableUserCommand request, CancellationToken cancellationToken)
    {
        if (request.UserId == currentUserService.UserId)
        {
            return Result.Failure(Error.Validation("DisableUser.CannotDisableSelf", "You cannot disable your own account."));
        }

        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("DisableUser.NotFound", "User was not found."));
        }

        user.Disable();
        userRepository.Update(user);

        return Result.Success();
    }
}
