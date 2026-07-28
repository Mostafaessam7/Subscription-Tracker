using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Reports;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Reports.ExportSubscriptionsCsv;

public sealed record ExportSubscriptionsCsvQuery(
    string? SearchTerm, Guid? CategoryId, Guid? TagId, SubscriptionStatus? Status) : IQuery<ReportFileDto>;
