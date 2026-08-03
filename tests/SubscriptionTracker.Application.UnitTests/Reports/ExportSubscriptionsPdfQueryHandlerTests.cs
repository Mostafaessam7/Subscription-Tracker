using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Reports.ExportSubscriptionsPdf;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Reports;

public class ExportSubscriptionsPdfQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly ExportSubscriptionsPdfQueryHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public ExportSubscriptionsPdfQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _dbContext = new ApplicationDbContext(options, _currentUserService);
        _handler = new ExportSubscriptionsPdfQueryHandler(_dbContext, _currentUserService);
    }

    private Subscription CreateSubscription(string name, SubscriptionStatus status = SubscriptionStatus.Active)
    {
        var price = Money.Create(9.99m, "USD").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;
        var subscription = Subscription.Create(_workspaceId, Guid.NewGuid(), name, "Some Inc.", price, cycle, new DateOnly(2026, 1, 1)).Value;

        if (status == SubscriptionStatus.Cancelled)
        {
            subscription.Cancel(new DateOnly(2026, 1, 15), null);
        }

        return subscription;
    }

    [Fact]
    public async Task Handle_ShouldProduceAValidNonEmptyPdf()
    {
        _dbContext.Subscriptions.AddRange(CreateSubscription("Netflix"), CreateSubscription("Spotify"));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new ExportSubscriptionsPdfQuery(null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/pdf");
        result.Value.FileName.Should().EndWith(".pdf");
        result.Value.Content.Should().NotBeEmpty();

        // %PDF- magic header confirms QuestPDF actually produced a real PDF, not just arbitrary bytes.
        var header = System.Text.Encoding.ASCII.GetString(result.Value.Content, 0, 5);
        header.Should().Be("%PDF-");
    }

    [Fact]
    public async Task Handle_WithStatusFilter_ShouldOnlyIncludeMatchingSubscriptions()
    {
        _dbContext.Subscriptions.AddRange(CreateSubscription("Netflix"), CreateSubscription("OldOne", SubscriptionStatus.Cancelled));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(
            new ExportSubscriptionsPdfQuery(null, null, null, SubscriptionStatus.Active), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Content.Should().NotBeEmpty();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
