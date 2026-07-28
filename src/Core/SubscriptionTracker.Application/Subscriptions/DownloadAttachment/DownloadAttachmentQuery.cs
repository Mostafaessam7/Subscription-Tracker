using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Subscriptions.DownloadAttachment;

public sealed record DownloadAttachmentQuery(Guid SubscriptionId, Guid AttachmentId) : IQuery<AttachmentContentDto>;

public sealed record AttachmentContentDto(string FileName, string ContentType, byte[] Content);
