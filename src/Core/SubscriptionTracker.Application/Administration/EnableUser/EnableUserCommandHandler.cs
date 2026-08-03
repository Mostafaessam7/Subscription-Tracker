using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Administration.EnableUser;

public sealed class EnableUserCommandHandler(IRepository<User, Guid> userRepository) : ICommandHandler<EnableUserCommand>
{
    public async Task<Result> Handle(EnableUserCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("EnableUser.NotFound", "User was not found."));
        }

        user.Enable();
        userRepository.Update(user);

        return Result.Success();
    }
}
