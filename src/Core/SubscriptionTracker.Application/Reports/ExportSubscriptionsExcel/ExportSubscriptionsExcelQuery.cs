using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Reports;
using SubscriptionTracker.Domain.Subscriptions.Enums;

namespace SubscriptionTracker.Application.Reports.ExportSubscriptionsExcel;

public sealed record ExportSubscriptionsExcelQuery(
    string? SearchTerm, Guid? CategoryId, Guid? TagId, SubscriptionStatus? Status) : IQuery<ReportFileDto>;
