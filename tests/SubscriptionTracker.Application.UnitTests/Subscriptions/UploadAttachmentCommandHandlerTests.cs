using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Subscriptions.UploadAttachment;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.UnitTests.Subscriptions;

public class UploadAttachmentCommandHandlerTests
{
    private readonly IRepository<Subscription, Guid> _subscriptionRepository = Substitute.For<IRepository<Subscription, Guid>>();
    private readonly IFileStorageService _fileStorageService = Substitute.For<IFileStorageService>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly UploadAttachmentCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();
    private readonly Guid _userId = Guid.NewGuid();

    public UploadAttachmentCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _currentUserService.UserId.Returns(_userId);
        _handler = new UploadAttachmentCommandHandler(_subscriptionRepository, _fileStorageService, _currentUserService);
    }

    private Subscription CreateSubscription()
    {
        var price = Money.Create(9.99m, "USD").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;
        return Subscription.Create(_workspaceId, _userId, "Netflix", "Netflix Inc.", price, cycle, new DateOnly(2026, 1, 1)).Value;
    }

    [Fact]
    public async Task Handle_WithValidFile_ShouldSaveAndAttach()
    {
        var subscription = CreateSubscription();
        _subscriptionRepository.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);
        _fileStorageService.SaveAsync(Arg.Any<byte[]>(), "receipt.pdf", Arg.Any<CancellationToken>()).Returns("abc123.pdf");

        var command = new UploadAttachmentCommand(subscription.Id, "receipt.pdf", "application/pdf", [1, 2, 3]);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        subscription.Attachments.Should().ContainSingle(a => a.FileName == "receipt.pdf" && a.StoragePath == "abc123.pdf");
        _subscriptionRepository.Received(1).Update(subscription);
    }

    [Fact]
    public async Task Handle_WhenSubscriptionBelongsToAnotherWorkspace_ShouldFailWithoutSaving()
    {
        var subscription = Subscription.Create(
            Guid.NewGuid(), _userId, "Netflix", "Netflix Inc.", Money.Create(9.99m, "USD").Value,
            BillingCycle.Create(BillingFrequency.Monthly).Value, new DateOnly(2026, 1, 1)).Value;

        _subscriptionRepository.GetByIdAsync(subscription.Id, Arg.Any<CancellationToken>()).Returns(subscription);

        var command = new UploadAttachmentCommand(subscription.Id, "receipt.pdf", "application/pdf", [1, 2, 3]);
        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UploadAttachment.NotFound");
        await _fileStorageService.DidNotReceive().SaveAsync(Arg.Any<byte[]>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
