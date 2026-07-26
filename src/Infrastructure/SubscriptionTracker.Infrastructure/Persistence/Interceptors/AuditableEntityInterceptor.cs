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
            if (reference.TargetEntry!.Entity is ISoftDeletable softDeletable)
            {
                reference.TargetEntry.State = EntityState.Modified;
                softDeletable.Delete(DateTimeOffset.UtcNow, null);
            }
        }
    }
}
