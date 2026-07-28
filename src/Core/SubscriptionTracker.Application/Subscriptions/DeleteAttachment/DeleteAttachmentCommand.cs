using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Subscriptions.DeleteAttachment;

public sealed record DeleteAttachmentCommand(Guid SubscriptionId, Guid AttachmentId) : ICommand;
