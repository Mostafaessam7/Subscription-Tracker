using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.GetMyWorkspace;

public sealed record GetMyWorkspaceQuery : IQuery<WorkspaceDto>;
