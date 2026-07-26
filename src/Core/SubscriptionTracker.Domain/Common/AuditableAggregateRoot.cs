namespace SubscriptionTracker.Domain.Common;

public abstract class AuditableAggregateRoot<TId> : AggregateRoot<TId>, IAuditableEntity, ISoftDeletable
    where TId : notnull
{
    protected AuditableAggregateRoot(TId id)
        : base(id)
    {
    }

    protected AuditableAggregateRoot()
    {
    }

    public DateTimeOffset CreatedAtUtc { get; private set; }
    public string? CreatedBy { get; private set; }
    public DateTimeOffset? LastModifiedAtUtc { get; private set; }
    public string? LastModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public string? DeletedBy { get; private set; }

    public void SetCreated(DateTimeOffset occurredOnUtc, string? actor)
    {
        CreatedAtUtc = occurredOnUtc;
        CreatedBy = actor;
    }

    public void SetModified(DateTimeOffset occurredOnUtc, string? actor)
    {
        LastModifiedAtUtc = occurredOnUtc;
        LastModifiedBy = actor;
    }

    public void Delete(DateTimeOffset occurredOnUtc, string? actor)
    {
        IsDeleted = true;
        DeletedAtUtc = occurredOnUtc;
        DeletedBy = actor;
    }

    public void Restore()
    {
        IsDeleted = false;
        DeletedAtUtc = null;
        DeletedBy = null;
    }
}
