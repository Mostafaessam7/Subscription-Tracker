using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.DownloadAttachment;

public sealed class DownloadAttachmentQueryHandler(
    IRepository<Subscription, Guid> subscriptionRepository, IFileStorageService fileStorageService, ICurrentUserService currentUserService)
    : IQueryHandler<DownloadAttachmentQuery, AttachmentContentDto>
{
    public async Task<Result<AttachmentContentDto>> Handle(DownloadAttachmentQuery request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure<AttachmentContentDto>(Error.NotFound("DownloadAttachment.NotFound", "Subscription was not found."));
        }

        var attachment = subscription.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId);
        if (attachment is null)
        {
            return Result.Failure<AttachmentContentDto>(
                Error.NotFound("DownloadAttachment.AttachmentNotFound", "Attachment was not found."));
        }

        var content = await fileStorageService.ReadAsync(attachment.StoragePath, cancellationToken);

        return Result.Success(new AttachmentContentDto(attachment.FileName, attachment.ContentType, content));
    }
}
