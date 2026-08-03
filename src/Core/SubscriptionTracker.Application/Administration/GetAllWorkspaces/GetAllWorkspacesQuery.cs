using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Administration.GetAllWorkspaces;

/// <summary>System-admin only: every workspace across every tenant.</summary>
public sealed record GetAllWorkspacesQuery : IQuery<IReadOnlyList<AdminWorkspaceSummaryDto>>;
