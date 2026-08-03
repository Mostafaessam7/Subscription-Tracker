using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Notifications.GetMyNotifications;
using SubscriptionTracker.Domain.Notifications;
using SubscriptionTracker.Infrastructure.Persistence;

namespace SubscriptionTracker.Application.UnitTests.Notifications;

public class GetMyNotificationsQueryHandlerTests : IDisposable
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly GetMyNotificationsQueryHandler _handler;
    private readonly Guid _userId = Guid.NewGuid();
    private readonly Guid _workspaceId = Guid.NewGuid();

    public GetMyNotificationsQueryHandlerTests()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _currentUserService.UserId.Returns(_userId);
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _dbContext = new ApplicationDbContext(options, _currentUserService);
        _handler = new GetMyNotificationsQueryHandler(_dbContext, _currentUserService);
    }

    [Fact]
    public async Task Handle_ShouldOnlyReturnTheCurrentUsersNotifications_NewestFirst()
    {
        var otherUserId = Guid.NewGuid();

        _dbContext.Notifications.Add(Notification.Create(
            _workspaceId, _userId, NotificationType.RenewalReminder, "Older", "msg", null, DateTimeOffset.UtcNow.AddHours(-2)));
        _dbContext.Notifications.Add(Notification.Create(
            _workspaceId, _userId, NotificationType.BudgetAlert, "Newer", "msg", null, DateTimeOffset.UtcNow.AddHours(-1)));
        _dbContext.Notifications.Add(Notification.Create(
            _workspaceId, otherUserId, NotificationType.General, "NotMine", "msg", null, DateTimeOffset.UtcNow));

        await _dbContext.SaveChangesAsync();

        var result = await _handler.Handle(new GetMyNotificationsQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items[0].Title.Should().Be("Newer");
        result.Value.Items[1].Title.Should().Be("Older");
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        GC.SuppressFinalize(this);
    }
}
