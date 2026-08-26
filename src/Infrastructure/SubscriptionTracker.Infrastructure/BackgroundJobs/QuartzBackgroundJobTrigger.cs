using Quartz;
using SubscriptionTracker.Application.Abstractions;

namespace SubscriptionTracker.Infrastructure.BackgroundJobs;

/// <summary>Bridges <see cref="IBackgroundJobTrigger"/> to Quartz's <see cref="ISchedulerFactory"/> so
/// application code can fire one of the daily jobs registered in <c>DependencyInjection.AddBackgroundJobs</c>
/// on demand, e.g. from a system-admin "run now" action, without waiting for its cron schedule.</summary>
public sealed class QuartzBackgroundJobTrigger(ISchedulerFactory schedulerFactory) : IBackgroundJobTrigger
{
    public IReadOnlyCollection<string> JobNames { get; } =
        ["renewal-reminder", "auto-renewal", "expire-subscriptions", "budget-alert", "purge-soft-deleted"];

    public async Task<bool> TriggerAsync(string jobName, CancellationToken cancellationToken)
    {
        if (!JobNames.Contains(jobName))
        {
            return false;
        }

        var scheduler = await schedulerFactory.GetScheduler(cancellationToken);
        var jobKey = new JobKey(jobName);

        if (!await scheduler.CheckExists(jobKey, cancellationToken))
        {
            return false;
        }

        await scheduler.TriggerJob(jobKey, cancellationToken);
        return true;
    }
}
