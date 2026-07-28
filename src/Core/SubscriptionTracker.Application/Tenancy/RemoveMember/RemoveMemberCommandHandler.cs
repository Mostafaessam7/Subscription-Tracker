using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.RemoveMember;

public sealed class RemoveMemberCommandHandler(
    IRepository<Workspace, Guid> workspaceRepository, ICurrentUserService currentUserService, TimeProvider timeProvider)
    : ICommandHandler<RemoveMemberCommand>
{
    public async Task<Result> Handle(RemoveMemberCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure(
                Error.Unauthorized("RemoveMember.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var workspace = await workspaceRepository.GetByIdAsync(currentUserService.WorkspaceId.Value, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure(Error.NotFound("RemoveMember.WorkspaceNotFound", "Workspace was not found."));
        }

        var result = workspace.RemoveMember(request.MemberId, timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result;
        }

        workspaceRepository.Update(workspace);

        return Result.Success();
    }
}
