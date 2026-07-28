using ClosedXML.Excel;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Reports.ExportSubscriptionsExcel;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Reports;

public class ExportSubscriptionsExcelQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly ExportSubscriptionsExcelQueryHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public ExportSubscriptionsExcelQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ApplicationDbContext(options);

        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new ExportSubscriptionsExcelQueryHandler(_dbContext, _currentUserService);
    }

    private Subscription CreateSubscription(string name) =>
        Subscription.Create(
            _workspaceId, Guid.NewGuid(), name, "Some Inc.", Money.Create(9.99m, "USD").Value,
            BillingCycle.Create(BillingFrequency.Monthly).Value, new DateOnly(2026, 1, 1)).Value;

    [Fact]
    public async Task Handle_ShouldProduceAReadableWorkbookWithOneRowPerSubscription()
    {
        _dbContext.Subscriptions.AddRange(CreateSubscription("Netflix"), CreateSubscription("Spotify"));
        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new ExportSubscriptionsExcelQuery(null, null, null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.ContentType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        result.Value.FileName.Should().EndWith(".xlsx");

        using var stream = new MemoryStream(result.Value.Content);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheet("Subscriptions");

        worksheet.Cell(1, 1).GetString().Should().Be("Name");
        worksheet.Cell(2, 1).GetString().Should().Be("Netflix");
        worksheet.Cell(3, 1).GetString().Should().Be("Spotify");
        worksheet.Cell(4, 1).IsEmpty().Should().BeTrue();
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
