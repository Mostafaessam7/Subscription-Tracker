using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Reports.ExportSubscriptionsCsv;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Reports;

public class ExportSubscriptionsCsvQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly ExportSubscriptionsCsvQueryHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public ExportSubscriptionsCsvQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _dbContext = new ApplicationDbContext(options, _currentUserService);
        _handler = new ExportSubscriptionsCsvQueryHandler(_dbContext, _currentUserService);
    }

    private Subscription CreateSubscription(string name, string provider = "Some Inc.", SubscriptionStatus status = SubscriptionStatus.Active)
    {
        var price = Money.Create(9.99m, "USD").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;
        var subscription = Subscription.Create(_workspaceId, Guid.NewGuid(), name, provider, price, cycle, new DateOnly(2026, 1, 1)).Value;

        if (status == SubscriptionStatus.Cancelled)
        {
            subscription.Cancel(new DateOnly(2026, 1, 15), null);
        }

        return subscription;
    }

    [Fact]
    public async Task Handle_ShouldIncludeHeaderAndOneRowPerMatchingSubscription()
    {
        _dbContext.Subscriptions.AddRange(CreateSubscription("Netflix"), CreateSubscription("Spotify"));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new ExportSubscriptionsCsvQuery(null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("text/csv");
        result.Value.FileName.Should().EndWith(".csv");

        var lines = Encoding.UTF8.GetString(result.Value.Content).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(3); // header + 2 rows
        lines[0].Should().StartWith("Name,Provider,Category");
        lines.Should().Contain(l => l.StartsWith("Netflix,"));
        lines.Should().Contain(l => l.StartsWith("Spotify,"));
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldOnlyIncludeMatchingSubscriptions()
    {
        _dbContext.Subscriptions.AddRange(CreateSubscription("Netflix"), CreateSubscription("OldOne", status: SubscriptionStatus.Cancelled));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new ExportSubscriptionsCsvQuery(null, null, null, SubscriptionStatus.Active), CancellationToken.None);

        var lines = Encoding.UTF8.GetString(result.Value.Content).Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2); // header + 1 row
        lines[1].Should().StartWith("Netflix,");
    }

    [Fact]
    public async Task Handle_WithCommaInProviderName_ShouldQuoteTheField()
    {
        _dbContext.Subscriptions.Add(CreateSubscription("Acme", "Acme, Inc."));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new ExportSubscriptionsCsvQuery(null, null, null, null), CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.Value.Content);
        csv.Should().Contain("\"Acme, Inc.\"");
    }

    [Theory]
    [InlineData("=HYPERLINK(\"http://evil.example/\",\"click me\")")]
    [InlineData("+1+1")]
    [InlineData("-1+1")]
    [InlineData("@SUM(1,1)")]
    public async Task Handle_WithASubscriptionNameThatLooksLikeAFormula_ShouldNeutralizeItWithALeadingQuote(string maliciousName)
    {
        // CSV Injection / Formula Injection (OWASP, CWE-1236): a cell starting with =, +, -, or @ gets
        // executed as a formula by Excel/Sheets/LibreOffice on open. Name has no character restrictions
        // (see CreateSubscriptionCommandValidator), so this is a reachable exploit, not just theoretical.
        _dbContext.Subscriptions.Add(CreateSubscription(maliciousName));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new ExportSubscriptionsCsvQuery(null, null, null, null), CancellationToken.None);

        var csv = Encoding.UTF8.GetString(result.Value.Content);
        var dataLine = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries)[1];
        var firstField = dataLine.TrimStart('"'); // the Name column may or may not be comma-quoted depending on content
        firstField.Should().StartWith("'" + maliciousName[..1], "a leading quote neutralizes the formula trigger without changing the visible text");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
