using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure.BackgroundJobs;

/// <summary>Runs daily and renews every active, auto-renewing subscription whose next renewal date has arrived.</summary>
[DisallowConcurrentExecution]
public sealed class AutoRenewalJob(
    ApplicationDbContext dbContext, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<AutoRenewalJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var now = timeProvider.GetUtcNow();
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        var cancellationToken = context.CancellationToken;

        var dueSubscriptions = await dbContext.Subscriptions
            .Where(s =>
                s.Status == SubscriptionStatus.Active &&
                s.AutoRenewal &&
                s.NextRenewalDate != null &&
                s.NextRenewalDate <= today)
            .ToListAsync(cancellationToken);

        var renewedCount = 0;

        foreach (var subscription in dueSubscriptions)
        {
            var result = subscription.Renew(now);
            if (result.IsSuccess)
            {
                renewedCount++;
            }
            else
            {
                logger.LogWarning(
                    "Auto-renewal skipped for subscription {SubscriptionId}: {Error}", subscription.Id, result.Error.Message);
            }
        }

        if (renewedCount > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Auto-renewed {Count} subscription(s)", renewedCount);
        }
    }
}
