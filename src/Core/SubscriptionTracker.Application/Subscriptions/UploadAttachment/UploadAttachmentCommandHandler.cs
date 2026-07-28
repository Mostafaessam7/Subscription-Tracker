using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.UploadAttachment;

public sealed class UploadAttachmentCommandHandler(
    IRepository<Subscription, Guid> subscriptionRepository, IFileStorageService fileStorageService, ICurrentUserService currentUserService)
    : ICommandHandler<UploadAttachmentCommand, Guid>
{
    public async Task<Result<Guid>> Handle(UploadAttachmentCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("UploadAttachment.NotSignedIn", "You must be signed in."));
        }

        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure<Guid>(Error.NotFound("UploadAttachment.NotFound", "Subscription was not found."));
        }

        var storagePath = await fileStorageService.SaveAsync(request.Content, request.FileName, cancellationToken);

        var attachmentResult = subscription.AddAttachment(
            request.FileName, request.ContentType, request.Content.Length, storagePath, currentUserService.UserId.Value);

        if (attachmentResult.IsFailure)
        {
            await fileStorageService.DeleteAsync(storagePath, cancellationToken);
            return Result.Failure<Guid>(attachmentResult.Error);
        }

        subscriptionRepository.Update(subscription);

        return Result.Success(attachmentResult.Value.Id);
    }
}
