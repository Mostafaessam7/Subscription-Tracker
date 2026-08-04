namespace SubscriptionTracker.Application.Abstractions;

/// <summary>Lets application code fire an already-scheduled background job on demand (e.g. from a
/// system-admin "run now" action) without the Application layer taking a dependency on Quartz.</summary>
public interface IBackgroundJobTrigger
{
    /// <summary>Names of every job available to trigger, e.g. "renewal-reminder".</summary>
    IReadOnlyCollection<string> JobNames { get; }

    /// <summary>Fires the named job immediately. Returns false if no job with that name is registered.</summary>
    Task<bool> TriggerAsync(string jobName, CancellationToken cancellationToken);
}
