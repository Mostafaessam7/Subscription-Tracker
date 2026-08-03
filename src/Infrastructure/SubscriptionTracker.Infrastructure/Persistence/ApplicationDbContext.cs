using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Auditing;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options, ICurrentUserService currentUserService)
    : DbContext(options), IUnitOfWork, IApplicationDbContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<PaymentMethod> PaymentMethods => Set<PaymentMethod>();
    public DbSet<Subscription> Subscriptions => Set<Subscription>();
    public DbSet<Budget> Budgets => Set<Budget>();
    public DbSet<AuditLogEntry> AuditLogs => Set<AuditLogEntry>();

    IQueryable<User> IApplicationDbContext.Users => Users.AsNoTracking();
    IQueryable<Role> IApplicationDbContext.Roles => Roles.AsNoTracking();
    IQueryable<Workspace> IApplicationDbContext.Workspaces => Workspaces.AsNoTracking();
    IQueryable<Category> IApplicationDbContext.Categories => Categories.AsNoTracking();
    IQueryable<Tag> IApplicationDbContext.Tags => Tags.AsNoTracking();
    IQueryable<PaymentMethod> IApplicationDbContext.PaymentMethods => PaymentMethods.AsNoTracking();
    IQueryable<Subscription> IApplicationDbContext.Subscriptions => Subscriptions.AsNoTracking();
    IQueryable<Budget> IApplicationDbContext.Budgets => Budgets.AsNoTracking();
    IQueryable<AuditLogEntry> IApplicationDbContext.AuditLogs => AuditLogs.AsNoTracking();

    /// <summary>
    /// Backs the tenant-isolation query filters below. Read lazily (not captured) so it reflects whatever the
    /// current DI-scoped ICurrentUserService reports at query-execution time, not at DbContext construction time.
    /// </summary>
    private Guid? CurrentWorkspaceId => currentUserService.WorkspaceId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        ApplyTenantIsolationFilters(modelBuilder);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Defense-in-depth: every command/query handler already filters these workspace-scoped entities explicitly
    /// by WorkspaceId, but a global filter means a handler that forgets to do so can no longer leak another
    /// tenant's rows. Deliberately scoped to the five simple workspace-owned aggregates (Category/Tag/
    /// PaymentMethod/Subscription/Budget) - NOT to User/Role/Workspace/AuditLogEntry, several of which have
    /// legitimate cross-workspace query paths (GetMyWorkspaces, GetPendingInvitations, the workspace switcher,
    /// system-role lookups) that a workspace-keyed filter would silently break.
    ///
    /// The `CurrentWorkspaceId == null` branch is required for code paths with no per-request tenant context -
    /// Quartz background jobs and startup/migration tooling - which legitimately sweep every workspace. Every
    /// interactive API request always has an authenticated WorkspaceId claim by the time it reaches a handler
    /// (enforced by [Authorize]/[HasPermission]), so this escape hatch never weakens protection for real
    /// user-facing requests - it only avoids breaking code that was never meant to be tenant-scoped.
    ///
    /// EF Core allows only one HasQueryFilter per entity, so this REPLACES (not adds to) the soft-delete-only
    /// filter each entity's IEntityTypeConfiguration already declares - the `!e.IsDeleted` clause is repeated
    /// here for that reason, not duplicated by accident.
    /// </summary>
    private void ApplyTenantIsolationFilters(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().HasQueryFilter(e => !e.IsDeleted && (CurrentWorkspaceId == null || e.WorkspaceId == CurrentWorkspaceId));
        modelBuilder.Entity<Tag>().HasQueryFilter(e => !e.IsDeleted && (CurrentWorkspaceId == null || e.WorkspaceId == CurrentWorkspaceId));
        modelBuilder.Entity<PaymentMethod>().HasQueryFilter(e => !e.IsDeleted && (CurrentWorkspaceId == null || e.WorkspaceId == CurrentWorkspaceId));
        modelBuilder.Entity<Subscription>().HasQueryFilter(e => !e.IsDeleted && (CurrentWorkspaceId == null || e.WorkspaceId == CurrentWorkspaceId));
        modelBuilder.Entity<Budget>().HasQueryFilter(e => !e.IsDeleted && (CurrentWorkspaceId == null || e.WorkspaceId == CurrentWorkspaceId));
    }

    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);
}
