using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Identity.SetupTwoFactor;

public sealed class SetupTwoFactorQueryHandler(
    IRepository<User, Guid> userRepository, ICurrentUserService currentUserService, ITwoFactorService twoFactorService)
    : IQueryHandler<SetupTwoFactorQuery, SetupTwoFactorResponse>
{
    private const string Issuer = "Subscription Tracker";

    public async Task<Result<SetupTwoFactorResponse>> Handle(SetupTwoFactorQuery request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Failure<SetupTwoFactorResponse>(
                Error.Unauthorized("SetupTwoFactor.NotSignedIn", "You must be signed in."));
        }

        var user = await userRepository.GetByIdAsync(currentUserService.UserId.Value, cancellationToken);
        if (user is null)
        {
            return Result.Failure<SetupTwoFactorResponse>(Error.NotFound("SetupTwoFactor.NotFound", "User was not found."));
        }

        // The secret is not persisted here - it's only saved once EnableTwoFactorCommand confirms the user
        // actually scanned it correctly (see that handler), so an abandoned setup never leaves 2FA half-configured.
        var secret = twoFactorService.GenerateSecret();
        var provisioningUri = twoFactorService.GetProvisioningUri(secret, user.Email.Value, Issuer);

        return Result.Success(new SetupTwoFactorResponse(secret, provisioningUri));
    }
}
