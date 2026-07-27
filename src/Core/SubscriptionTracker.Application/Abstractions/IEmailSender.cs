namespace SubscriptionTracker.Application.Abstractions;

public interface IEmailSender
{
    Task SendEmailVerificationAsync(
        string toEmail, string recipientName, Guid userId, string verificationToken, CancellationToken cancellationToken = default);

    Task SendPasswordResetAsync(
        string toEmail, string recipientName, Guid userId, string resetToken, CancellationToken cancellationToken = default);

    Task SendRenewalReminderAsync(
        string toEmail, string recipientName, string subscriptionName, DateOnly renewalDate, CancellationToken cancellationToken = default);

    Task SendBudgetOverspendAlertAsync(
        string toEmail, string recipientName, string budgetName, decimal spentAmount, decimal budgetAmount, string currencyCode,
        CancellationToken cancellationToken = default);
}
