using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Quartz;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Budgets;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.BackgroundJobs;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.BackgroundJobs;

/// <summary>
/// Actually *executes* each Quartz job's Execute() method (against a real InMemory-backed ApplicationDbContext)
/// rather than just confirming the scheduler registers them - closing the "job execution never verified" gap
/// flagged in PROJECT_STATUS.md/HANDOVER.md. Faster and more repeatable than the documented manual-trigger
/// workaround (editing a cron expression to fire in ~1 minute and watching logs).
/// </summary>
public class BackgroundJobExecutionTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IJobExecutionContext _jobContext = Substitute.For<IJobExecutionContext>();
    private readonly Guid _workspaceId = Guid.NewGuid();

    public BackgroundJobExecutionTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // Jobs sweep across every workspace with no per-request tenant context, same as production Quartz
        // execution - an unconfigured ICurrentUserService reports a null WorkspaceId, which is exactly the
        // escape hatch ApplicationDbContext's tenant-isolation query filters use to mean "don't filter".
        _dbContext = new ApplicationDbContext(options, Substitute.For<ICurrentUserService>());
    }

    private Subscription CreateSubscription(
        DateOnly startDate, bool autoRenewal, SubscriptionStatus status = SubscriptionStatus.Active, DateOnly? trialEndDate = null)
    {
        var owner = SubscriptionTracker.Domain.Identity.User.Register(
            Email.Create($"{Guid.NewGuid():N}@example.com").Value, "hash", "Jane", "Doe").Value;
        _dbContext.Users.Add(owner);

        var price = Money.Create(9.99m, "USD").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;
        var subscription = Subscription.Create(
            _workspaceId, owner.Id, "Netflix", "Netflix Inc.", price, cycle, startDate, trialEndDate, autoRenewal).Value;

        _dbContext.Subscriptions.Add(subscription);
        return subscription;
    }

    [Fact]
    public async Task RenewalReminderJob_WhenReminderThresholdMatchesToday_ShouldEmailTheOwner()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var today = new DateOnly(2026, 1, 1);
        var timeProvider = FixedTimeProvider(today);

        var subscription = CreateSubscription(today.AddDays(-30), autoRenewal: true);
        subscription.SetReminderDaysBeforeRenewal([7]);
        // NextRenewalDate for a subscription started 30 days ago on a monthly cycle lands 1 month after start;
        // force it to exactly 7 days from "today" so the reminder-day match fires deterministically.
        typeof(Subscription).GetProperty(nameof(Subscription.NextRenewalDate))!
            .SetValue(subscription, today.AddDays(7));
        await _dbContext.SaveChangesAsync();

        var job = new RenewalReminderJob(
            _dbContext, emailSender, Substitute.For<INotificationPublisher>(), timeProvider, NullLogger<RenewalReminderJob>.Instance);
        await job.Execute(_jobContext);

        await emailSender.Received(1).SendRenewalReminderAsync(
            Arg.Any<string>(), Arg.Any<string>(), "Netflix", today.AddDays(7), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AutoRenewalJob_WithDueAutoRenewingSubscription_ShouldAdvanceNextRenewalDate()
    {
        var today = new DateOnly(2026, 1, 1);
        var timeProvider = FixedTimeProvider(today);

        var subscription = CreateSubscription(today.AddMonths(-1), autoRenewal: true);
        var originalRenewalDate = subscription.NextRenewalDate;
        await _dbContext.SaveChangesAsync();

        var job = new AutoRenewalJob(_dbContext, _dbContext, timeProvider, NullLogger<AutoRenewalJob>.Instance);
        await job.Execute(_jobContext);

        var reloaded = await _dbContext.Subscriptions.FirstAsync(s => s.Id == subscription.Id);
        reloaded.NextRenewalDate.Should().NotBe(originalRenewalDate);
        reloaded.RenewalHistory.Should().ContainSingle();
    }

    [Fact]
    public async Task ExpireSubscriptionsJob_WithNonAutoRenewingPastDueSubscription_ShouldMarkExpired()
    {
        var today = new DateOnly(2026, 3, 1);
        var timeProvider = FixedTimeProvider(today);

        // Started well over a month ago, non-auto-renewing, so NextRenewalDate is already in the past relative to "today".
        var subscription = CreateSubscription(new DateOnly(2026, 1, 1), autoRenewal: false);
        await _dbContext.SaveChangesAsync();

        var job = new ExpireSubscriptionsJob(_dbContext, _dbContext, timeProvider, NullLogger<ExpireSubscriptionsJob>.Instance);
        await job.Execute(_jobContext);

        var reloaded = await _dbContext.Subscriptions.FirstAsync(s => s.Id == subscription.Id);
        reloaded.Status.Should().Be(SubscriptionStatus.Expired);
    }

    [Fact]
    public async Task BudgetAlertJob_WhenSpendCrossesThreshold_ShouldEmailTheWorkspaceOwner()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var owner = SubscriptionTracker.Domain.Identity.User.Register(
            Email.Create($"{Guid.NewGuid():N}@example.com").Value, "hash", "Jane", "Doe").Value;
        var role = SubscriptionTracker.Domain.Identity.Role.Create("Owner", null, _workspaceId).Value;
        var workspace = SubscriptionTracker.Domain.Tenancy.Workspace.Create(
            "Acme", owner.Id, role.Id, DateTimeOffset.UtcNow, _workspaceId).Value;

        _dbContext.Users.Add(owner);
        _dbContext.Workspaces.Add(workspace);

        // 9.99/month against a 10/month budget with an 80% threshold - comfortably over.
        var budget = Budget.Create(_workspaceId, "Streaming", Money.Create(10m, "USD").Value, BudgetPeriod.Monthly, null, 80).Value;
        _dbContext.Budgets.Add(budget);

        CreateSubscription(new DateOnly(2026, 1, 1), autoRenewal: true);
        await _dbContext.SaveChangesAsync();

        var job = new BudgetAlertJob(
            _dbContext, emailSender, Substitute.For<INotificationPublisher>(), Substitute.For<IExchangeRateProvider>(),
            NullLogger<BudgetAlertJob>.Instance);
        await job.Execute(_jobContext);

        await emailSender.Received(1).SendBudgetOverspendAlertAsync(
            owner.Email.Value, owner.FirstName, "Streaming", Arg.Any<decimal>(), 10m, "USD", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task BudgetAlertJob_WithACrossCurrencySubscription_ShouldConvertItBeforeComparingAgainstTheThreshold()
    {
        var emailSender = Substitute.For<IEmailSender>();
        var exchangeRateProvider = Substitute.For<IExchangeRateProvider>();
        exchangeRateProvider.GetRate("EUR", "USD").Returns(1.1m);

        var owner = SubscriptionTracker.Domain.Identity.User.Register(
            Email.Create($"{Guid.NewGuid():N}@example.com").Value, "hash", "Jane", "Doe").Value;
        var role = SubscriptionTracker.Domain.Identity.Role.Create("Owner", null, _workspaceId).Value;
        var workspace = SubscriptionTracker.Domain.Tenancy.Workspace.Create(
            "Acme", owner.Id, role.Id, DateTimeOffset.UtcNow, _workspaceId).Value;
        _dbContext.Users.Add(owner);
        _dbContext.Workspaces.Add(workspace);

        var budget = Budget.Create(_workspaceId, "Streaming", Money.Create(10m, "USD").Value, BudgetPeriod.Monthly, null, 80).Value;
        _dbContext.Budgets.Add(budget);

        // 9 EUR/month * 1.1 EUR->USD ≈ 9.90 USD, comfortably over the 80%-of-10 threshold - would NOT cross
        // the threshold if the currency mismatch caused this subscription to be silently skipped (pre-fix
        // behavior), so this test fails loudly if conversion regresses back to "different currency = ignore".
        var price = Money.Create(9m, "EUR").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;
        var subscription = Subscription.Create(_workspaceId, owner.Id, "Netflix", "Netflix Inc.", price, cycle, new DateOnly(2026, 1, 1)).Value;
        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync();

        var job = new BudgetAlertJob(
            _dbContext, emailSender, Substitute.For<INotificationPublisher>(), exchangeRateProvider, NullLogger<BudgetAlertJob>.Instance);
        await job.Execute(_jobContext);

        await emailSender.Received(1).SendBudgetOverspendAlertAsync(
            owner.Email.Value, owner.FirstName, "Streaming", Arg.Any<decimal>(), 10m, "USD", Arg.Any<CancellationToken>());
    }

    private static FakeTimeProvider FixedTimeProvider(DateOnly date) => new(date.ToDateTime(TimeOnly.MinValue));

    private sealed class FakeTimeProvider(DateTime instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(instant, TimeSpan.Zero);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
