using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Security;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;

namespace SubscriptionTracker.Application.Identity.ForgotPassword;

public sealed class ForgotPasswordCommandHandler(
    IRepository<User, Guid> userRepository, IEmailSender emailSender, TimeProvider timeProvider)
    : ICommandHandler<ForgotPasswordCommand>
{
    private static readonly TimeSpan ResetTokenLifetime = TimeSpan.FromHours(1);

    public async Task<Result> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            // Do not leak whether the email is well-formed or registered; always report success.
            return Result.Success();
        }

        var user = await userRepository.FirstOrDefaultAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken);
        if (user is null)
        {
            return Result.Success();
        }

        var rawToken = SecureTokenGenerator.Generate();
        user.IssueVerificationToken(
            VerificationTokenPurpose.PasswordReset,
            SecureTokenGenerator.Hash(rawToken),
            timeProvider.GetUtcNow().Add(ResetTokenLifetime));

        userRepository.Update(user);

        await emailSender.SendPasswordResetAsync(user.Email.Value, user.FullName, user.Id, rawToken, cancellationToken);

        return Result.Success();
    }
}
