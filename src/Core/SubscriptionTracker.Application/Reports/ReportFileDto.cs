namespace SubscriptionTracker.Application.Reports;

public sealed record ReportFileDto(string FileName, string ContentType, byte[] Content);
