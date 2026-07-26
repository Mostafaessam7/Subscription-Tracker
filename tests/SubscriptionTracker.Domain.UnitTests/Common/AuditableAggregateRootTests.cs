using FluentAssertions;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.UnitTests.Common;

public class AuditableAggregateRootTests
{
    private sealed class TestAggregate(Guid id) : AuditableAggregateRoot<Guid>(id);

    [Fact]
    public void SetCreated_ShouldPopulateAuditFields()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        aggregate.SetCreated(now, "tester");

        aggregate.CreatedAtUtc.Should().Be(now);
        aggregate.CreatedBy.Should().Be("tester");
    }

    [Fact]
    public void Delete_ShouldMarkAsDeleted()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        var now = DateTimeOffset.UtcNow;

        aggregate.Delete(now, "tester");

        aggregate.IsDeleted.Should().BeTrue();
        aggregate.DeletedAtUtc.Should().Be(now);
        aggregate.DeletedBy.Should().Be("tester");
    }

    [Fact]
    public void Restore_ShouldClearDeletionState()
    {
        var aggregate = new TestAggregate(Guid.NewGuid());
        aggregate.Delete(DateTimeOffset.UtcNow, "tester");

        aggregate.Restore();

        aggregate.IsDeleted.Should().BeFalse();
        aggregate.DeletedAtUtc.Should().BeNull();
        aggregate.DeletedBy.Should().BeNull();
    }
}
