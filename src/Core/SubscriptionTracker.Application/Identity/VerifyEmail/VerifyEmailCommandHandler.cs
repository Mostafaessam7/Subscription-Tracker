using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Security;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Identity.VerifyEmail;

public sealed class VerifyEmailCommandHandler(IRepository<User, Guid> userRepository) : ICommandHandler<VerifyEmailCommand>
{
    public async Task<Result> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var user = await userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Failure(Error.NotFound("VerifyEmail.UserNotFound", "User was not found."));
        }

        var tokenHash = SecureTokenGenerator.Hash(request.Token);
        var consumeResult = user.ConsumeVerificationToken(tokenHash, VerificationTokenPurpose.EmailVerification);
        if (consumeResult.IsFailure)
        {
            return Result.Failure(consumeResult.Error);
        }

        user.VerifyEmail();
        userRepository.Update(user);

        return Result.Success();
    }
}
