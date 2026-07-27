using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Infrastructure.Persistence.Repositories;

internal sealed class Repository<TAggregate, TId>(ApplicationDbContext dbContext) : IRepository<TAggregate, TId>
    where TAggregate : AggregateRoot<TId>
    where TId : notnull
{
    private readonly DbSet<TAggregate> _dbSet = dbContext.Set<TAggregate>();

    // A plain query (not FindAsync) is used so that AutoInclude()-configured navigations are always loaded,
    // matching the DDD expectation that fetching an aggregate root loads its full consistency boundary.
    public async Task<TAggregate?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await _dbSet.FirstOrDefaultAsync(e => e.Id.Equals(id), cancellationToken);

    public async Task<TAggregate?> FirstOrDefaultAsync(Specification<TAggregate> specification, CancellationToken cancellationToken = default) =>
        await SpecificationEvaluator.Apply(_dbSet.AsQueryable(), specification).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TAggregate>> ListAsync(Specification<TAggregate> specification, CancellationToken cancellationToken = default) =>
        await SpecificationEvaluator.Apply(_dbSet.AsQueryable(), specification).ToListAsync(cancellationToken);

    public async Task<int> CountAsync(Specification<TAggregate> specification, CancellationToken cancellationToken = default) =>
        await SpecificationEvaluator.Apply(_dbSet.AsQueryable(), specification).CountAsync(cancellationToken);

    public void Add(TAggregate aggregate) => _dbSet.Add(aggregate);

    public void Update(TAggregate aggregate) => _dbSet.Update(aggregate);

    public void Remove(TAggregate aggregate) => _dbSet.Remove(aggregate);
}
