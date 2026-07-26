namespace SubscriptionTracker.Domain.Common;

public interface IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);

    Task<TAggregate?> FirstOrDefaultAsync(Specification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TAggregate>> ListAsync(Specification<TAggregate> specification, CancellationToken cancellationToken = default);

    Task<int> CountAsync(Specification<TAggregate> specification, CancellationToken cancellationToken = default);

    void Add(TAggregate aggregate);

    void Update(TAggregate aggregate);

    void Remove(TAggregate aggregate);
}
