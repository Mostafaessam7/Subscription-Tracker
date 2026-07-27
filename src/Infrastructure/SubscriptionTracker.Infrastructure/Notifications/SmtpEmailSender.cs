using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;
using SubscriptionTracker.Application.Abstractions;

namespace SubscriptionTracker.Infrastructure.Notifications;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    private readonly SmtpOptions _options = options.Value;

    public Task SendEmailVerificationAsync(
        string toEmail, string recipientName, Guid userId, string verificationToken, CancellationToken cancellationToken = default)
    {
        var link = $"{_options.FrontendBaseUrl.TrimEnd('/')}/auth/verify-email?userId={userId}&token={Uri.EscapeDataString(verificationToken)}";

        var body = $"""
            <p>Hi {recipientName},</p>
            <p>Welcome to Subscription Tracker. Please verify your email address to activate your account:</p>
            <p><a href="{link}">Verify my email</a></p>
            <p>This link expires in 24 hours. If you did not create this account, you can safely ignore this email.</p>
            """;

        return SendAsync(toEmail, "Verify your email address", body, cancellationToken);
    }

    public Task SendPasswordResetAsync(
        string toEmail, string recipientName, Guid userId, string resetToken, CancellationToken cancellationToken = default)
    {
        var link = $"{_options.FrontendBaseUrl.TrimEnd('/')}/auth/reset-password?userId={userId}&token={Uri.EscapeDataString(resetToken)}";

        var body = $"""
            <p>Hi {recipientName},</p>
            <p>We received a request to reset your password. Click below to choose a new one:</p>
            <p><a href="{link}">Reset my password</a></p>
            <p>This link expires in 1 hour. If you did not request this, you can safely ignore this email.</p>
            """;

        return SendAsync(toEmail, "Reset your password", body, cancellationToken);
    }

    public Task SendRenewalReminderAsync(
        string toEmail, string recipientName, string subscriptionName, DateOnly renewalDate, CancellationToken cancellationToken = default)
    {
        var body = $"""
            <p>Hi {recipientName},</p>
            <p><strong>{subscriptionName}</strong> is set to renew on <strong>{renewalDate:yyyy-MM-dd}</strong>.</p>
            <p>Log in to Subscription Tracker if you'd like to review, pause, or cancel it beforehand.</p>
            """;

        return SendAsync(toEmail, $"Upcoming renewal: {subscriptionName}", body, cancellationToken);
    }

    public Task SendBudgetOverspendAlertAsync(
        string toEmail, string recipientName, string budgetName, decimal spentAmount, decimal budgetAmount, string currencyCode,
        CancellationToken cancellationToken = default)
    {
        var percentage = budgetAmount == 0 ? 0 : Math.Round(spentAmount / budgetAmount * 100m, 1);

        var body = $"""
            <p>Hi {recipientName},</p>
            <p>Your budget <strong>{budgetName}</strong> has reached <strong>{percentage}%</strong> of its limit:
            {spentAmount:0.00} {currencyCode} spent of {budgetAmount:0.00} {currencyCode}.</p>
            <p>Log in to Subscription Tracker to review your spending.</p>
            """;

        return SendAsync(toEmail, $"Budget alert: {budgetName}", body, cancellationToken);
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.Host))
        {
            logger.LogWarning("SMTP host is not configured; skipping email '{Subject}' to {ToEmail}", subject, toEmail);
            return;
        }

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(_options.FromName, _options.FromAddress));
        message.To.Add(MailboxAddress.Parse(toEmail));
        message.Subject = subject;
        message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

        using var client = new SmtpClient();

        try
        {
            await client.ConnectAsync(
                _options.Host, _options.Port, _options.UseStartTls ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto,
                cancellationToken);

            if (!string.IsNullOrWhiteSpace(_options.Username))
            {
                await client.AuthenticateAsync(_options.Username, _options.Password, cancellationToken);
            }

            await client.SendAsync(message, cancellationToken);
        }
        finally
        {
            if (client.IsConnected)
            {
                await client.DisconnectAsync(true, cancellationToken);
            }
        }
    }
}
