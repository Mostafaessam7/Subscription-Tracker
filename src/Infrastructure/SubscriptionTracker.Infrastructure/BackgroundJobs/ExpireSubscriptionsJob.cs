using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Quartz;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Infrastructure.BackgroundJobs;

/// <summary>Runs daily and marks non-auto-renewing subscriptions as expired once their renewal date has passed.</summary>
[DisallowConcurrentExecution]
public sealed class ExpireSubscriptionsJob(
    ApplicationDbContext dbContext, IUnitOfWork unitOfWork, TimeProvider timeProvider, ILogger<ExpireSubscriptionsJob> logger) : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var today = DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime);
        var cancellationToken = context.CancellationToken;

        var candidates = await dbContext.Subscriptions
            .Where(s =>
                s.Status == SubscriptionStatus.Active &&
                !s.AutoRenewal &&
                s.NextRenewalDate != null &&
                s.NextRenewalDate < today)
            .ToListAsync(cancellationToken);

        foreach (var subscription in candidates)
        {
            subscription.MarkExpiredIfPastRenewalDate(today);
        }

        if (candidates.Count > 0)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Expired {Count} subscription(s)", candidates.Count);
        }
    }
}
