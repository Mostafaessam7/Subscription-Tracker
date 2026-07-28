using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.ChangeMemberRole;

public sealed class ChangeMemberRoleCommandHandler(
    IRepository<Workspace, Guid> workspaceRepository, IRepository<Role, Guid> roleRepository, ICurrentUserService currentUserService)
    : ICommandHandler<ChangeMemberRoleCommand>
{
    public async Task<Result> Handle(ChangeMemberRoleCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure(
                Error.Unauthorized("ChangeMemberRole.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null || (role.WorkspaceId is not null && role.WorkspaceId != currentUserService.WorkspaceId))
        {
            return Result.Failure(Error.NotFound("ChangeMemberRole.RoleNotFound", "Role was not found."));
        }

        var workspace = await workspaceRepository.GetByIdAsync(currentUserService.WorkspaceId.Value, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure(Error.NotFound("ChangeMemberRole.WorkspaceNotFound", "Workspace was not found."));
        }

        var result = workspace.ChangeMemberRole(request.MemberId, request.RoleId);
        if (result.IsFailure)
        {
            return result;
        }

        workspaceRepository.Update(workspace);

        return Result.Success();
    }
}
