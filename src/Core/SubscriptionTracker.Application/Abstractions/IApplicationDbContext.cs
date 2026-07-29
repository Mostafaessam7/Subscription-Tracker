using SubscriptionTracker.Domain.Auditing;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Subscriptions;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Abstractions;

/// <summary>Read-only query surface for the persistence layer. Queries bypass repositories/aggregates by design.</summary>
public interface IApplicationDbContext
{
    IQueryable<User> Users { get; }
    IQueryable<Role> Roles { get; }
    IQueryable<Workspace> Workspaces { get; }
    IQueryable<Category> Categories { get; }
    IQueryable<Tag> Tags { get; }
    IQueryable<PaymentMethod> PaymentMethods { get; }
    IQueryable<Subscription> Subscriptions { get; }
    IQueryable<Budget> Budgets { get; }
    IQueryable<AuditLogEntry> AuditLogs { get; }
}
