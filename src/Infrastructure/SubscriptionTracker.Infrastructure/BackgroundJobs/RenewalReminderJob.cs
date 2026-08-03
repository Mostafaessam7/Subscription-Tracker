using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Notifications;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Infrastructure.BackgroundJobs;

/// <summary>Runs daily and emails subscription owners whose next renewal date matches one of their configured reminder-day thresholds.</summary>
[DisallowConcurrentExecution]
public sealed class RenewalReminderJob(
    IApplicationDbContext dbContext, IEmailSender emailSender, INotificationPublisher notificationPublisher,
    TimeProvider timeProvider, ILogger<RenewalReminderJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var cancellationToken = context.CancellationToken;

        var candidates = await dbContext.Subscriptions
            .Where(s => (s.Status == SubscriptionStatus.Active || s.Status == SubscriptionStatus.Trial) && s.NextRenewalDate != null)
            .Select(s => new { s.Id, s.WorkspaceId, s.Name, s.OwnerId, s.NextRenewalDate, s.ReminderDaysBeforeRenewal })
            .ToListAsync(cancellationToken);

        var due = candidates
            .Where(s => s.ReminderDaysBeforeRenewal.Contains(s.NextRenewalDate!.Value.DayNumber - today.DayNumber))
            .ToList();

        if (due.Count == 0)
        {
            return;
        }

        var ownerIds = due.Select(s => s.OwnerId).Distinct().ToList();
        var owners = await dbContext.Users
            .Where(u => ownerIds.Contains(u.Id))
            .Select(u => new { u.Id, Email = u.Email.Value, u.FirstName })
            .ToDictionaryAsync(u => u.Id, cancellationToken);

        foreach (var subscription in due)
        {
            if (!owners.TryGetValue(subscription.OwnerId, out var owner))
            {
                continue;
            }

            await emailSender.SendRenewalReminderAsync(
                owner.Email, owner.FirstName, subscription.Name, subscription.NextRenewalDate!.Value, cancellationToken);

            await notificationPublisher.PublishAsync(
                subscription.WorkspaceId, subscription.OwnerId, NotificationType.RenewalReminder,
                "Upcoming renewal", $"{subscription.Name} renews on {subscription.NextRenewalDate!.Value:yyyy-MM-dd}.",
                subscription.Id, cancellationToken);
        }

        logger.LogInformation("Sent {Count} renewal reminder email(s)", due.Count);
    }
}
