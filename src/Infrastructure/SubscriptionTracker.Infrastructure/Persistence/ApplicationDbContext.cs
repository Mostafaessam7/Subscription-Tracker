using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Infrastructure.Persistence;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
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

    IQueryable<User> IApplicationDbContext.Users => Users.AsNoTracking();
    IQueryable<Role> IApplicationDbContext.Roles => Roles.AsNoTracking();
    IQueryable<Workspace> IApplicationDbContext.Workspaces => Workspaces.AsNoTracking();
    IQueryable<Category> IApplicationDbContext.Categories => Categories.AsNoTracking();
    IQueryable<Tag> IApplicationDbContext.Tags => Tags.AsNoTracking();
    IQueryable<PaymentMethod> IApplicationDbContext.PaymentMethods => PaymentMethods.AsNoTracking();
    IQueryable<Subscription> IApplicationDbContext.Subscriptions => Subscriptions.AsNoTracking();
    IQueryable<Budget> IApplicationDbContext.Budgets => Budgets.AsNoTracking();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    async Task<int> IUnitOfWork.SaveChangesAsync(CancellationToken cancellationToken) =>
        await SaveChangesAsync(cancellationToken);
}
