namespace SubscriptionTracker.Application.Abstractions;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(
        string toEmail, string recipientName, Guid userId, string verificationToken, CancellationToken cancellationToken = default);

    /// <summary>Sent instead of creating a duplicate account when someone submits the registration form with an
    /// email that already has one - see <c>RegisterUserCommandHandler</c> for why the API response is identical
    /// either way (non-enumerable, same as <c>ForgotPasswordCommandHandler</c>). Lets the real owner know what
    /// happened instead of leaving them silently confused about a "verify your email" message that never
    /// arrives.</summary>
    Task SendDuplicateRegistrationAttemptAsync(
        string toEmail, string recipientName, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string toEmail, string recipientName, Guid userId, string resetToken, CancellationToken cancellationToken = default);

    Task SendRenewalReminderAsync(
        string toEmail, string recipientName, string subscriptionName, DateOnly renewalDate, CancellationToken cancellationToken = default);

    Task SendBudgetOverspendAlertAsync(
        string toEmail, string recipientName, string budgetName, decimal spentAmount, decimal budgetAmount, string currencyCode,
        CancellationToken cancellationToken = default);

    Task SendWorkspaceInvitationAsync(
        string toEmail, string workspaceName, string inviterName, string invitationToken, CancellationToken cancellationToken = default);
}
