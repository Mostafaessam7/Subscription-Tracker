namespace SubscriptionTracker.Application.Abstractions;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(string toEmail, string recipientName, string verificationToken, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(string toEmail, string recipientName, string resetToken, CancellationToken cancellationToken = default);

    Task SendRenewalReminderAsync(string toEmail, string recipientName, string subscriptionName, DateOnly renewalDate, CancellationToken cancellationToken = default);
}
