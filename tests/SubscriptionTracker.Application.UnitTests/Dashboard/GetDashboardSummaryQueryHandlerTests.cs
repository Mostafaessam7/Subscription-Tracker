using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Dashboard.GetDashboardSummary;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Dashboard;

public class GetDashboardSummaryQueryHandlerTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 6, 15);

    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetDashboardSummaryQueryHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public GetDashboardSummaryQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _dbContext = new ApplicationDbContext(options, _currentUserService);
        _handler = new GetDashboardSummaryQueryHandler(_dbContext, _currentUserService, new FakeTimeProvider(Today));
    }

    private Subscription CreateSubscription(
        string name,
        decimal amount = 10m,
        BillingFrequency frequency = BillingFrequency.Monthly,
        int? customIntervalDays = null,
        DateOnly? startDate = null,
        DateOnly? trialEndDate = null)
    {
        var price = Money.Create(amount, "USD").Value;
        var cycle = BillingCycle.Create(frequency, customIntervalDays).Value;
        return Subscription.Create(
            _workspaceId, Guid.NewGuid(), name, "Some Inc.", price, cycle, startDate ?? Today, trialEndDate).Value;
    }

    [Fact]
    public async Task Handle_ShouldCountAllSubscriptionsRegardlessOfPageSizeCap()
    {
        // 101 subscriptions - more than GetSubscriptionsQuery's 100-item page-size cap - to prove this
        // endpoint doesn't have the same undercounting bug the old client-side computation had.
        for (var i = 0; i < 101; i++)
        {
            _dbContext.Subscriptions.Add(CreateSubscription($"Sub {i}", frequency: BillingFrequency.Yearly));
        }
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSubscriptions.Should().Be(101);
        result.Value.ActiveCount.Should().Be(101);
    }

    [Fact]
    public async Task Handle_ShouldOnlyCountActiveAndTrialTowardEstimatedMonthlySpend()
    {
        var active = CreateSubscription("Active", amount: 10m, frequency: BillingFrequency.Monthly);
        var cancelled = CreateSubscription("Cancelled", amount: 999m, frequency: BillingFrequency.Monthly);
        cancelled.Cancel(Today, null);

        _dbContext.Subscriptions.AddRange(active, cancelled);
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        // BudgetSpendCalculator annualizes-then-divides using average days-per-month/year, so a plain
        // Monthly subscription doesn't round-trip to exactly its own amount - see BudgetSpendCalculator.
        result.Value.EstimatedMonthlySpend.Should().BeApproximately(10m, 0.01m);
        result.Value.ActiveCount.Should().Be(1);
        result.Value.TotalSubscriptions.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldOnlyIncludeRenewalsWithinTheThirtyDayWindow()
    {
        var soon = CreateSubscription("Soon", frequency: BillingFrequency.Custom, customIntervalDays: 5, startDate: Today);
        var farAway = CreateSubscription("FarAway", frequency: BillingFrequency.Custom, customIntervalDays: 45, startDate: Today);

        _dbContext.Subscriptions.AddRange(soon, farAway);
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.Value.UpcomingRenewals.Should().ContainSingle(r => r.SubscriptionId == soon.Id);
        result.Value.UpcomingRenewals.Single().DaysUntil.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldCapUpcomingRenewalsAtFiveOrderedByDate()
    {
        for (var i = 1; i <= 8; i++)
        {
            _dbContext.Subscriptions.Add(
                CreateSubscription($"Sub {i}", frequency: BillingFrequency.Custom, customIntervalDays: i, startDate: Today));
        }
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.Value.UpcomingRenewals.Should().HaveCount(5);
        result.Value.UpcomingRenewals.Select(r => r.DaysUntil).Should().BeInAscendingOrder();
        result.Value.UpcomingRenewals.First().DaysUntil.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldGroupBillableSubscriptionsByFrequency()
    {
        _dbContext.Subscriptions.AddRange(
            CreateSubscription("A", frequency: BillingFrequency.Monthly),
            CreateSubscription("B", frequency: BillingFrequency.Monthly),
            CreateSubscription("C", frequency: BillingFrequency.Yearly));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.Value.SpendByFrequency.Should().HaveCount(2);
        result.Value.SpendByFrequency.First().Frequency.Should().Be(BillingFrequency.Monthly);
        result.Value.SpendByFrequency.First().Count.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldOnlyIncludeSubscriptionsFromTheCurrentWorkspace()
    {
        var otherWorkspaceSubscription = Subscription.Create(
            Guid.NewGuid(), Guid.NewGuid(), "Someone Else's", "Some Inc.",
            Money.Create(10m, "USD").Value, BillingCycle.Create(BillingFrequency.Monthly).Value, Today).Value;

        _dbContext.Subscriptions.AddRange(CreateSubscription("Mine"), otherWorkspaceSubscription);
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetDashboardSummaryQuery(), CancellationToken.None);

        result.Value.TotalSubscriptions.Should().Be(1);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }

    private sealed class FakeTimeProvider(DateTime instant) : TimeProvider
    {
        public FakeTimeProvider(DateOnly date) : this(date.ToDateTime(TimeOnly.MinValue))
        {
        }

        public override DateTimeOffset GetUtcNow() => new(instant, TimeSpan.Zero);
    }
}
