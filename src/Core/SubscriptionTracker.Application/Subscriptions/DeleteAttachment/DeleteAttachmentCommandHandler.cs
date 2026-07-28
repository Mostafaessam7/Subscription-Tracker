using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.DeleteAttachment;

public sealed class DeleteAttachmentCommandHandler(
    IRepository<Subscription, Guid> subscriptionRepository, IFileStorageService fileStorageService, ICurrentUserService currentUserService)
    : ICommandHandler<DeleteAttachmentCommand>
{
    public async Task<Result> Handle(DeleteAttachmentCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("DeleteAttachment.NotFound", "Subscription was not found."));
        }

        var attachment = subscription.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId);
        if (attachment is null)
        {
            return Result.Failure(Error.NotFound("DeleteAttachment.AttachmentNotFound", "Attachment was not found."));
        }

        var storagePath = attachment.StoragePath;

        var removeResult = subscription.RemoveAttachment(request.AttachmentId);
        if (removeResult.IsFailure)
        {
            return removeResult;
        }

        subscriptionRepository.Update(subscription);
        await fileStorageService.DeleteAsync(storagePath, cancellationToken);

        return Result.Success();
    }
}
