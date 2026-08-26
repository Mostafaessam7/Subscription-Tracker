using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Infrastructure.BackgroundJobs;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Api.IntegrationTests;

/// <summary>
/// Runs the purge job against a real SQL Server LocalDB instance (via the same migrated-schema factory every
/// other integration test uses) specifically to prove <c>ExecuteDeleteAsync</c> works against Budget, whose
/// <c>Amount</c> is an owned <c>Money</c> value object mapped into the *same table* (OwnsOne, no separate
/// table) - EF Core has historically restricted bulk ExecuteDelete/ExecuteUpdate on entities involved in table
/// splitting, so this is exactly the case a purely-unit-tested (mocked IApplicationDbContext) test could not
/// have caught; see HANDOVER.md §5 for the same class of gap that bit AuditableEntityInterceptor's own
/// owned-value-object handling previously.
/// </summary>
public sealed class PurgeSoftDeletedRecordsJobTests : IClassFixture<ApiWebApplicationFactory>, IAsyncLifetime
{
    private readonly ApiWebApplicationFactory _factory;
    private AsyncServiceScope _scope = default!;

    public PurgeSoftDeletedRecordsJobTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    public Task InitializeAsync()
    {
        _scope = _factory.Services.CreateAsyncScope();
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => _scope.DisposeAsync().AsTask();

    [Fact]
    public async Task RunAsync_PurgesOnlyBudgetsSoftDeletedPastRetention()
    {
        var dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var appDbContext = _scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["DataRetention:SoftDeleteRetentionDays"] = "90" })
            .Build();

        var oldDeletedBudget = Budget.Create(Guid.NewGuid(), "Old deleted", Money.Create(10, "USD").Value, BudgetPeriod.Monthly).Value;
        var recentlyDeletedBudget = Budget.Create(Guid.NewGuid(), "Recently deleted", Money.Create(20, "USD").Value, BudgetPeriod.Monthly).Value;
        var stillActiveBudget = Budget.Create(Guid.NewGuid(), "Still active", Money.Create(30, "USD").Value, BudgetPeriod.Monthly).Value;

        dbContext.Budgets.AddRange(oldDeletedBudget, recentlyDeletedBudget, stillActiveBudget);
        await dbContext.SaveChangesAsync();

        // Soft-delete two of the three directly via the domain method (not dbContext.Remove(), which the
        // AuditableEntityInterceptor would convert right back into exactly this same state) with a controlled
        // DeletedAtUtc, so one lands outside the retention window and one lands inside it.
        oldDeletedBudget.Delete(DateTimeOffset.UtcNow.AddDays(-91), "test");
        recentlyDeletedBudget.Delete(DateTimeOffset.UtcNow.AddDays(-10), "test");
        dbContext.Entry(oldDeletedBudget).State = EntityState.Modified;
        dbContext.Entry(recentlyDeletedBudget).State = EntityState.Modified;
        await dbContext.SaveChangesAsync();

        var job = new PurgeSoftDeletedRecordsJob(
            appDbContext, configuration, TimeProvider.System, NullLogger<PurgeSoftDeletedRecordsJob>.Instance);

        var purgedCount = await job.RunAsync(CancellationToken.None);

        purgedCount.Should().Be(1);

        var remainingIds = await dbContext.Budgets.IgnoreQueryFilters().Select(b => b.Id).ToListAsync();
        remainingIds.Should().NotContain(oldDeletedBudget.Id, "it was soft-deleted 91 days ago, past the 90-day retention window");
        remainingIds.Should().Contain(recentlyDeletedBudget.Id, "it was only soft-deleted 10 days ago, still within retention");
        remainingIds.Should().Contain(stillActiveBudget.Id, "it was never deleted at all");
    }
}
