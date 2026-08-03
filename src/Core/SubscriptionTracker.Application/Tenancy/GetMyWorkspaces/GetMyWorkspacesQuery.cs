using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.GetMyWorkspaces;

public sealed record GetMyWorkspacesQuery : IQuery<IReadOnlyList<MyWorkspaceSummaryDto>>;
