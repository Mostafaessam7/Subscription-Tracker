using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandler(
    IRepository<Role, Guid> roleRepository, IRepository<Workspace, Guid> workspaceRepository, ICurrentUserService currentUserService)
    : ICommandHandler<DeleteRoleCommand>
{
    public async Task<Result> Handle(DeleteRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound("DeleteRole.NotFound", "Role was not found."));
        }

        if (role.IsSystemRole)
        {
            return Result.Failure(Error.Forbidden("DeleteRole.SystemRoleImmutable", "System roles cannot be deleted."));
        }

        if (role.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("DeleteRole.NotFound", "Role was not found."));
        }

        var workspace = await workspaceRepository.GetByIdAsync(currentUserService.WorkspaceId!.Value, cancellationToken);
        var isInUse = workspace is not null
            && workspace.Members.Any(m => m.RoleId == role.Id && m.Status != WorkspaceMemberStatus.Removed);

        if (isInUse)
        {
            return Result.Failure(
                Error.Conflict("DeleteRole.InUse", "This role is currently assigned to one or more members and cannot be deleted."));
        }

        roleRepository.Remove(role);

        return Result.Success();
    }
}
