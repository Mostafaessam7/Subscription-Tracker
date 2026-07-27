using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.Tags.DeleteTag;

public sealed class DeleteTagCommandHandler(IRepository<Tag, Guid> tagRepository, ICurrentUserService currentUserService)
    : ICommandHandler<DeleteTagCommand>
{
    public async Task<Result> Handle(DeleteTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken);
        if (tag is null || tag.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("DeleteTag.NotFound", "Tag was not found."));
        }

        tagRepository.Remove(tag);

        return Result.Success();
    }
}
