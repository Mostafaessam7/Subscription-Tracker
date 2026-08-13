using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Budgets.GetBudgets;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Budgets;

public class GetBudgetsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly IExchangeRateProvider _exchangeRateProvider = Substitute.For<IExchangeRateProvider>();
    private readonly GetBudgetsQueryHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public GetBudgetsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _dbContext = new ApplicationDbContext(options, _currentUserService);
        _handler = new GetBudgetsQueryHandler(_dbContext, _currentUserService, _exchangeRateProvider);
    }

    private Subscription CreateSubscription(decimal amount, string currencyCode)
    {
        var price = Money.Create(amount, currencyCode).Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;
        return Subscription.Create(_workspaceId, Guid.NewGuid(), "Sub", "Some Inc.", price, cycle, new DateOnly(2026, 1, 1)).Value;
    }

    [Fact]
    public async Task Handle_WithACrossCurrencySubscription_ShouldConvertItUsingTheExchangeRateProvider()
    {
        var budget = Budget.Create(_workspaceId, "Streaming", Money.Create(100m, "USD").Value, BudgetPeriod.Monthly, null, 80).Value;
        _dbContext.Budgets.Add(budget);
        _dbContext.Subscriptions.Add(CreateSubscription(10m, "EUR"));
        await _dbContext.SaveChangesAsync();

        _exchangeRateProvider.GetRate("EUR", "USD").Returns(1.1m);

        var result = await _handler.Handle(new GetBudgetsQuery(), CancellationToken.None);

        // 10 EUR monthly * 1.1 EUR->USD rate ≈ 11 USD (up to BudgetSpendCalculator's annualize/de-annualize rounding).
        result.Value.Single().CurrentSpend.Should().BeApproximately(11m, 0.01m);
    }

    [Fact]
    public async Task Handle_WithACurrencyThatHasNoKnownRate_ShouldContributeZeroRatherThanThrowOrGuess()
    {
        var budget = Budget.Create(_workspaceId, "Streaming", Money.Create(100m, "USD").Value, BudgetPeriod.Monthly, null, 80).Value;
        _dbContext.Budgets.Add(budget);
        _dbContext.Subscriptions.Add(CreateSubscription(10m, "XYZ"));
        await _dbContext.SaveChangesAsync();

        _exchangeRateProvider.GetRate("XYZ", "USD").Returns((decimal?)null);

        var result = await _handler.Handle(new GetBudgetsQuery(), CancellationToken.None);

        result.Value.Single().CurrentSpend.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WithASameCurrencySubscription_ShouldNotCallTheExchangeRateProvider()
    {
        var budget = Budget.Create(_workspaceId, "Streaming", Money.Create(100m, "USD").Value, BudgetPeriod.Monthly, null, 80).Value;
        _dbContext.Budgets.Add(budget);
        _dbContext.Subscriptions.Add(CreateSubscription(10m, "USD"));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetBudgetsQuery(), CancellationToken.None);

        result.Value.Single().CurrentSpend.Should().BeApproximately(10m, 0.01m);
        _exchangeRateProvider.DidNotReceive().GetRate(Arg.Any<string>(), Arg.Any<string>());
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
