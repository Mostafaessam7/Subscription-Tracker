using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Subscriptions.DeleteAttachment;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.UnitTests.Subscriptions;

public class DeleteAttachmentCommandHandlerTests
{
    private readonly IRepository<Subscription, Guid> _subscriptionRepository = Substitute.For<IRepository<Subscription, Guid>>();
    private readonly IFileStorageService _fileStorageService = Substitute.For<IFileStorageService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly DeleteAttachmentCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public DeleteAttachmentCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new DeleteAttachmentCommandHandler(_subscriptionRepository, _fileStorageService, _currentUserService);
    }

    private Subscription CreateSubscriptionWithAttachment(out Guid attachmentId)
    {
        var price = Money.Create(9.99m, "USD").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;
        var subscription = Subscription.Create(
            _workspaceId, Guid.NewGuid(), "Netflix", "Netflix Inc.", price, cycle, new DateOnly(2026, 1, 1)).Value;

        var attachment = subscription.AddAttachment("receipt.pdf", "application/pdf", 3, "abc123.pdf", Guid.NewGuid()).Value;
        attachmentId = attachment.Id;
        return subscription;
    }

    [Fact]
    public async Task Handle_WithExistingAttachment_ShouldRemoveAndDeleteFromStorage()
    {
        var subscription = CreateSubscriptionWithAttachment(out var attachmentId);
        _subscriptionRepository.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var result = await _handler.Handle(new DeleteAttachmentCommand(subscription.Id, attachmentId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        subscription.Attachments.Should().BeEmpty();
        _subscriptionRepository.Received(1).Update(subscription);
        await _fileStorageService.Received(1).DeleteAsync("abc123.pdf", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithUnknownAttachmentId_ShouldFailWithoutTouchingStorage()
    {
        var subscription = CreateSubscriptionWithAttachment(out _);
        _subscriptionRepository.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var result = await _handler.Handle(new DeleteAttachmentCommand(subscription.Id, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DeleteAttachment.AttachmentNotFound");
        await _fileStorageService.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
