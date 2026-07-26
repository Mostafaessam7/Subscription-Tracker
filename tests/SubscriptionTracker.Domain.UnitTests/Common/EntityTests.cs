using FluentAssertions;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.UnitTests.Common;

public class EntityTests
{
    private sealed record TestEvent : DomainEvent;

    private sealed class TestEntity(Guid id) : Entity<Guid>(id)
    {
        public void Raise() => RaiseDomainEvent(new TestEvent());
    }

    [Fact]
    public void Equals_WithSameId_ShouldBeTrue()
    {
        var id = Guid.NewGuid();
        var a = new TestEntity(id);
        var b = new TestEntity(id);

        (a == b).Should().BeTrue();
    }

    [Fact]
    public void Equals_WithDifferentId_ShouldBeFalse()
    {
        var a = new TestEntity(Guid.NewGuid());
        var b = new TestEntity(Guid.NewGuid());

        (a == b).Should().BeFalse();
    }

    [Fact]
    public void RaiseDomainEvent_ShouldAddToDomainEvents()
    {
        var entity = new TestEntity(Guid.NewGuid());

        entity.Raise();

        entity.DomainEvents.Should().HaveCount(1);
        entity.DomainEvents.Single().Should().BeOfType<TestEvent>();
    }

    [Fact]
    public void ClearDomainEvents_ShouldEmptyCollection()
    {
        var entity = new TestEntity(Guid.NewGuid());
        entity.Raise();

        entity.ClearDomainEvents();

        entity.DomainEvents.Should().BeEmpty();
    }
}
