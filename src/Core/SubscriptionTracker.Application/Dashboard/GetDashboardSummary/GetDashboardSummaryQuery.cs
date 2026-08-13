using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Dashboard.GetDashboardSummary;

public sealed record GetDashboardSummaryQuery : IQuery<DashboardSummaryDto>;
