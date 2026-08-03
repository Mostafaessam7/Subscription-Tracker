using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Notifications.MarkNotificationAsRead;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Notifications;

namespace SubscriptionTracker.Application.UnitTests.Notifications;

public class MarkNotificationAsReadCommandHandlerTests
{
    private readonly IRepository<Notification, Guid> _notificationRepository = Substitute.For<IRepository<Notification, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly Guid _userId = Guid.NewGuid();
    private readonly MarkNotificationAsReadCommandHandler _handler;

    public MarkNotificationAsReadCommandHandlerTests()
    {
        _currentUserService.UserId.Returns(_userId);
        _handler = new MarkNotificationAsReadCommandHandler(_notificationRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_ShouldMarkOwnNotificationAsRead()
    {
        var notification = Notification.Create(
            Guid.NewGuid(), _userId, NotificationType.General, "Title", "msg", null, DateTimeOffset.UtcNow);
        _notificationRepository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _handler.Handle(new MarkNotificationAsReadCommand(notification.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        notification.IsRead.Should().BeTrue();
        _notificationRepository.Received(1).Update(notification);
    }

    [Fact]
    public async Task Handle_ForAnotherUsersNotification_ShouldFailWithNotFound()
    {
        var notification = Notification.Create(
            Guid.NewGuid(), Guid.NewGuid(), NotificationType.General, "Title", "msg", null, DateTimeOffset.UtcNow);
        _notificationRepository.GetByIdAsync(notification.Id, Arg.Any<CancellationToken>()).Returns(notification);

        var result = await _handler.Handle(new MarkNotificationAsReadCommand(notification.Id), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("MarkNotificationAsRead.NotFound");
    }
}
