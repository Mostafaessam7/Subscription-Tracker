using FluentAssertions;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Subscriptions.Enums;
using SubscriptionTracker.Domain.Subscriptions.Events;

namespace SubscriptionTracker.Domain.UnitTests.Subscriptions;

public class SubscriptionTests
{
    private static Subscription CreateActiveSubscription()
    {
        var price = Money.Create(9.99m, "USD").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;

        return Subscription
            .Create(Guid.NewGuid(), Guid.NewGuid(), "Netflix", "Netflix Inc.", price, cycle, new DateOnly(2026, 1, 1))
            .Value;
    }

    [Fact]
    public void Create_WithoutTrial_ShouldBeActiveAndRaiseEvent()
    {
        var subscription = CreateActiveSubscription();

        subscription.Status.Should().Be(SubscriptionStatus.Active);
        subscription.NextRenewalDate.Should().Be(new DateOnly(2026, 2, 1));
        subscription.DomainEvents.Should().ContainSingle(e => e is SubscriptionCreated);
    }

    [Fact]
    public void Create_WithTrial_ShouldBeInTrialStatus()
    {
        var price = Money.Create(9.99m, "USD").Value;
        var cycle = BillingCycle.Create(BillingFrequency.Monthly).Value;
        var startDate = new DateOnly(2026, 1, 1);
        var trialEndDate = new DateOnly(2026, 1, 14);

        var subscription = Subscription
            .Create(Guid.NewGuid(), Guid.NewGuid(), "Netflix", "Netflix Inc.", price, cycle, startDate, trialEndDate)
            .Value;

        subscription.Status.Should().Be(SubscriptionStatus.Trial);
        subscription.NextRenewalDate.Should().Be(trialEndDate);
    }

    [Fact]
    public void Renew_WhenActive_ShouldAdvanceRenewalDateAndRecordHistory()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.Renew(DateTimeOffset.UtcNow);

        result.IsSuccess.Should().BeTrue();
        subscription.NextRenewalDate.Should().Be(new DateOnly(2026, 3, 1));
        subscription.RenewalHistory.Should().ContainSingle();
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionRenewed);
    }

    [Fact]
    public void Renew_WhenCancelled_ShouldFail()
    {
        var subscription = CreateActiveSubscription();
        subscription.Cancel(new DateOnly(2026, 1, 15));

        var result = subscription.Renew(DateTimeOffset.UtcNow);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Cancel_ShouldSetStatusAndDisableAutoRenewal()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.Cancel(new DateOnly(2026, 1, 15), "No longer needed");

        result.IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Cancelled);
        subscription.AutoRenewal.Should().BeFalse();
        subscription.NextRenewalDate.Should().BeNull();
        subscription.DomainEvents.Should().Contain(e => e is SubscriptionCancelled);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ShouldFail()
    {
        var subscription = CreateActiveSubscription();
        subscription.Cancel(new DateOnly(2026, 1, 15));

        var result = subscription.Cancel(new DateOnly(2026, 1, 20));

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void Pause_ThenResume_ShouldRestoreActiveStatus()
    {
        var subscription = CreateActiveSubscription();

        subscription.Pause().IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Paused);

        subscription.Resume().IsSuccess.Should().BeTrue();
        subscription.Status.Should().Be(SubscriptionStatus.Active);
    }

    [Fact]
    public void Resume_WhenNotPaused_ShouldFail()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.Resume();

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void AddAttachment_ThenRemove_ShouldUpdateCollection()
    {
        var subscription = CreateActiveSubscription();

        var addResult = subscription.AddAttachment("invoice.pdf", "application/pdf", 1024, "/storage/invoice.pdf", Guid.NewGuid());
        addResult.IsSuccess.Should().BeTrue();
        subscription.Attachments.Should().ContainSingle();

        var removeResult = subscription.RemoveAttachment(addResult.Value.Id);
        removeResult.IsSuccess.Should().BeTrue();
        subscription.Attachments.Should().BeEmpty();
    }

    [Fact]
    public void SetReminderDaysBeforeRenewal_WithNegativeValue_ShouldFail()
    {
        var subscription = CreateActiveSubscription();

        var result = subscription.SetReminderDaysBeforeRenewal([1, -3, 7]);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public void ShareWith_ThenUnshare_ShouldUpdateSharedUsers()
    {
        var subscription = CreateActiveSubscription();
        var userId = Guid.NewGuid();

        subscription.ShareWith(userId);
        subscription.SharedUserIds.Should().Contain(userId);

        subscription.Unshare(userId);
        subscription.SharedUserIds.Should().NotContain(userId);
    }
}
