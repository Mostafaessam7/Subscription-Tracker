using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;

namespace SubscriptionTracker.Application.Identity.Logout;

public sealed class LogoutCommandHandler(IRepository<User, Guid> userRepository, IJwtTokenService jwtTokenService)
    : ICommandHandler<LogoutCommand>
{
    public async Task<Result> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);

        var user = await userRepository.FirstOrDefaultAsync(new UserByRefreshTokenHashSpecification(tokenHash), cancellationToken);
        if (user is null)
        {
            // Token is already invalid/unknown - logging out is idempotent from the caller's perspective.
            return Result.Success();
        }

        user.RevokeRefreshToken(tokenHash, revokedByIp: null);
        userRepository.Update(user);

        return Result.Success();
    }
}
