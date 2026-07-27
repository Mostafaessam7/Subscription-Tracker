using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Catalog.Specifications;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.Tags.UpdateTag;

public sealed class UpdateTagCommandHandler(IRepository<Tag, Guid> tagRepository, ICurrentUserService currentUserService)
    : ICommandHandler<UpdateTagCommand>
{
    public async Task<Result> Handle(UpdateTagCommand request, CancellationToken cancellationToken)
    {
        var tag = await tagRepository.GetByIdAsync(request.TagId, cancellationToken);
        if (tag is null || tag.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("UpdateTag.NotFound", "Tag was not found."));
        }

        var name = request.Name.Trim();

        var existing = await tagRepository.FirstOrDefaultAsync(
            new TagByWorkspaceAndNameSpecification(tag.WorkspaceId, name), cancellationToken);

        if (existing is not null && existing.Id != tag.Id)
        {
            return Result.Failure(Error.Conflict("UpdateTag.DuplicateName", "A tag with this name already exists."));
        }

        var renameResult = tag.Rename(name);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        tag.UpdateColor(request.Color);
        tagRepository.Update(tag);

        return Result.Success();
    }
}
