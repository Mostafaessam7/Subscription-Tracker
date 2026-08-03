using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.Roles.GetPermissionCatalog;

/// <summary>Static catalog of every permission code the role builder can grant - no DB access needed.</summary>
public sealed record GetPermissionCatalogQuery : IQuery<IReadOnlyList<PermissionCatalogEntryDto>>;
