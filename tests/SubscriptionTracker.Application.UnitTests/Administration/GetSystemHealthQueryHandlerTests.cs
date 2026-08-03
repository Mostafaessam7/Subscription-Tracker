using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Administration.GetSystemHealth;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Administration;

/// <summary>
/// Confirms GetSystemHealthQueryHandler's IgnoreQueryFilters() calls actually bypass ApplicationDbContext's
/// tenant-isolation query filters (added for defense-in-depth elsewhere) - without it, a system-wide count would
/// silently only ever see whichever single workspace happens to be "current" for the requesting admin.
/// </summary>
public class GetSystemHealthQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetSystemHealthQueryHandler _handler;

    public GetSystemHealthQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        // The admin's "current" workspace is A - if IgnoreQueryFilters() weren't applied, only workspace A's
        // subscription would be counted.
        var workspaceA = Guid.NewGuid();
        _currentUserService.WorkspaceId.Returns(workspaceA);
        _dbContext = new ApplicationDbContext(options, _currentUserService);
        _handler = new GetSystemHealthQueryHandler(_dbContext);

        var workspaceB = Guid.NewGuid();
        var price = Money.Create(9.99m, "USD").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;

        _dbContext.Subscriptions.Add(
            Subscription.Create(workspaceA, Guid.NewGuid(), "Netflix", "Netflix Inc.", price, cycle, new DateOnly(2026, 1, 1)).Value);
        _dbContext.Subscriptions.Add(
            Subscription.Create(workspaceB, Guid.NewGuid(), "Spotify", "Spotify AB", price, cycle, new DateOnly(2026, 1, 1)).Value);

        _dbContext.Users.Add(SubscriptionTracker.Domain.Identity.User.Register(
            Email.Create("a@example.com").Value, "hash", "A", "Owner").Value);
        _dbContext.Users.Add(SubscriptionTracker.Domain.Identity.User.Register(
            Email.Create("b@example.com").Value, "hash", "B", "Owner").Value);

        _dbContext.SaveChanges();
    }

    [Fact]
    public async Task Handle_ShouldCountAcrossEveryWorkspace_NotJustTheAdminsCurrentOne()
    {
        var result = await _handler.Handle(new GetSystemHealthQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalSubscriptions.Should().Be(2);
        result.Value.ActiveSubscriptions.Should().Be(2);
        result.Value.TotalUsers.Should().Be(2);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
