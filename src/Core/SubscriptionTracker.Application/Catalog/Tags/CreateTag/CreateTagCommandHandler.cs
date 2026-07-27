using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Catalog.Specifications;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.Tags.CreateTag;

public sealed class CreateTagCommandHandler(IRepository<Tag, Guid> tagRepository, ICurrentUserService currentUserService)
    : ICommandHandler<CreateTagCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateTagCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("CreateTag.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var workspaceId = currentUserService.WorkspaceId.Value;
        var name = request.Name.Trim();

        var existing = await tagRepository.FirstOrDefaultAsync(
            new TagByWorkspaceAndNameSpecification(workspaceId, name), cancellationToken);

        if (existing is not null)
        {
            return Result.Failure<Guid>(Error.Conflict("CreateTag.DuplicateName", "A tag with this name already exists."));
        }

        var tagResult = Tag.Create(workspaceId, name, request.Color);
        if (tagResult.IsFailure)
        {
            return Result.Failure<Guid>(tagResult.Error);
        }

        tagRepository.Add(tagResult.Value);

        return Result.Success(tagResult.Value.Id);
    }
}
