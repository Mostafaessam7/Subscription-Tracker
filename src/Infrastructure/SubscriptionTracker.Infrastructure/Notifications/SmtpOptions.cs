namespace SubscriptionTracker.Infrastructure.Notifications;

public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = string.Empty;
    public int Port { get; init; } = 587;
    public bool UseStartTls { get; init; } = true;
    public string Username { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public string FromAddress { get; init; } = "no-reply@subscriptiontracker.app";
    public string FromName { get; init; } = "Subscription Tracker";

    /// <summary>Base URL of the Angular frontend, used to build verification/reset deep links.</summary>
    public string FrontendBaseUrl { get; init; } = "https://localhost:4200";
}
