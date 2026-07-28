using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Subscriptions.UploadAttachment;

public sealed record UploadAttachmentCommand(Guid SubscriptionId, string FileName, string ContentType, byte[] Content) : ICommand<Guid>;
