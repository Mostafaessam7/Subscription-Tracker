using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Reports.ExportSubscriptionsPdf;

public sealed record ExportSubscriptionsPdfQuery(
    string? SearchTerm, Guid? CategoryId, Guid? TagId, SubscriptionStatus? Status) : IQuery<ReportFileDto>;
