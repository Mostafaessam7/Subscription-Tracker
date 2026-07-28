using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Identity.RevokeSession;

public sealed class RevokeSessionCommandHandler(IRepository<User, Guid> userRepository, ICurrentUserService currentUserService)
    : ICommandHandler<RevokeSessionCommand>
{
    public async Task<Result> Handle(RevokeSessionCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Failure(Error.Unauthorized("RevokeSession.NotSignedIn", "You must be signed in."));
        }

        var user = await userRepository.GetByIdAsync(currentUserService.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("RevokeSession.NotFound", "Session was not found."));
        }

        var result = user.RevokeRefreshTokenById(request.RefreshTokenId, request.RevokedByIp);
        if (result.IsFailure)
        {
            return result;
        }

        userRepository.Update(user);

        return Result.Success();
    }
}
