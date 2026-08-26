using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Quartz;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Infrastructure.BackgroundJobs;

/// <summary>
/// Runs weekly and permanently removes soft-deleted rows (Subscriptions/Budgets/Categories/Tags/PaymentMethods
/// - the five aggregates that carry a combined tenant+soft-delete EF Core global query filter, see
/// ApplicationDbContext.OnModelCreating) once they've been past their retention window, so the database
/// doesn't grow forever from every "delete" ever performed in the app's lifetime (every delete in this system
/// is a soft delete - AuditableEntityInterceptor.CascadeSoftDelete converts every EntityState.Deleted entry
/// back to Modified, so nothing is ever actually removed from the tables through the normal EF change-tracker
/// path; that's precisely why this job uses ExecuteDeleteAsync, which issues a raw SQL DELETE and bypasses
/// the change tracker - and hence the interceptor - entirely, rather than loading and Remove()-ing entities).
/// Retention defaults to 90 days, configurable via DataRetention:SoftDeleteRetentionDays; set it to 0 (or a
/// very large number) to effectively disable purging without removing the job itself.
/// </summary>
[DisallowConcurrentExecution]
public sealed class PurgeSoftDeletedRecordsJob(
    IApplicationDbContext dbContext, IConfiguration configuration, TimeProvider timeProvider, ILogger<PurgeSoftDeletedRecordsJob> logger)
    : IJob
{
    private const int DefaultRetentionDays = 90;

    public Task Execute(IJobExecutionContext context) => RunAsync(context.CancellationToken);

    /// <summary>The actual purge logic, factored out from <see cref="Execute"/> so it's callable (and its
    /// return value assertable) from a test without needing to fabricate a Quartz <see cref="IJobExecutionContext"/>.</summary>
    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        var retentionDays = configuration.GetValue("DataRetention:SoftDeleteRetentionDays", DefaultRetentionDays);
        var cutoff = timeProvider.GetUtcNow().AddDays(-retentionDays);

        var purged = new (string Name, int Count)[]
        {
            ("Subscriptions", await PurgeAsync(dbContext.Subscriptions, cutoff, cancellationToken)),
            ("Budgets", await PurgeAsync(dbContext.Budgets, cutoff, cancellationToken)),
            ("Categories", await PurgeAsync(dbContext.Categories, cutoff, cancellationToken)),
            ("Tags", await PurgeAsync(dbContext.Tags, cutoff, cancellationToken)),
            ("PaymentMethods", await PurgeAsync(dbContext.PaymentMethods, cutoff, cancellationToken)),
        };

        var total = purged.Sum(p => p.Count);
        if (total > 0)
        {
            var breakdown = string.Join(", ", purged.Where(p => p.Count > 0).Select(p => $"{p.Name}={p.Count}"));
            logger.LogInformation(
                "Purged {Total} soft-deleted record(s) older than {RetentionDays} day(s): {Breakdown}", total, retentionDays, breakdown);
        }

        return total;
    }

    /// <summary><see cref="EntityFrameworkQueryableExtensions.IgnoreQueryFilters{TEntity}"/> is required here -
    /// every one of these entities' normal query filter excludes soft-deleted rows (that's the whole point of
    /// a soft-delete filter), so without it this would always find zero candidates.</summary>
    private static Task<int> PurgeAsync<T>(IQueryable<T> query, DateTimeOffset cutoff, CancellationToken cancellationToken)
        where T : class, ISoftDeletable
        => query
            .IgnoreQueryFilters()
            .Where(e => e.IsDeleted && e.DeletedAtUtc != null && e.DeletedAtUtc < cutoff)
            .ExecuteDeleteAsync(cancellationToken);
}
