using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor(ICurrentUserService currentUserService, TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateAuditableEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditableEntities(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = timeProvider.GetUtcNow();
        var actor = currentUserService.Email ?? currentUserService.UserId?.ToString();

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.SetCreated(now, actor);
                    entry.Entity.SetModified(now, actor);
                    break;
                case EntityState.Modified:
                    entry.Entity.SetModified(now, actor);
                    break;
            }
        }

        foreach (var entry in context.ChangeTracker.Entries<ISoftDeletable>())
        {
            if (entry.State != EntityState.Deleted)
            {
                continue;
            }

            entry.State = EntityState.Modified;
            entry.Entity.Delete(now, actor);
            CascadeSoftDelete(entry);
        }
    }

    private static void CascadeSoftDelete(EntityEntry<ISoftDeletable> entry)
    {
        foreach (var reference in entry.References.Where(r => r.TargetEntry is { State: EntityState.Deleted }))
        {
            var targetEntry = reference.TargetEntry!;

            if (targetEntry.Entity is ISoftDeletable softDeletable)
            {
                targetEntry.State = EntityState.Modified;
                softDeletable.Delete(DateTimeOffset.UtcNow, null);
            }
            else if (targetEntry.Metadata.IsOwned())
            {
                // Owned value objects (e.g. Budget.Amount, a Money value object mapped via OwnsOne into the
                // same table) aren't soft-deletable themselves, but EF Core still tracks them as their own
                // ChangeTracker entry. Left at EntityState.Deleted while their owner above is flipped to
                // Modified, the two contradict each other and SaveChangesAsync throws - reproduced by actually
                // deleting a budget through the API, not caught by any handler-level unit test since those
                // never exercise the real interceptor pipeline against a real DbContext save.
                targetEntry.State = EntityState.Modified;
            }
        }
    }
}
