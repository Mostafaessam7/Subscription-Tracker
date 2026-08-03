using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Tenancy.Roles.UpdateRole;

public sealed class UpdateRoleCommandHandler(IRepository<Role, Guid> roleRepository, ICurrentUserService currentUserService)
    : ICommandHandler<UpdateRoleCommand>
{
    public async Task<Result> Handle(UpdateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null)
        {
            return Result.Failure(Error.NotFound("UpdateRole.NotFound", "Role was not found."));
        }

        if (role.IsSystemRole)
        {
            return Result.Failure(Error.Forbidden("UpdateRole.SystemRoleImmutable", "System roles cannot be edited."));
        }

        if (role.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("UpdateRole.NotFound", "Role was not found."));
        }

        var updateResult = role.UpdateDetails(request.Name, request.Description);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        var requestedCodes = request.PermissionCodes.Distinct().ToHashSet();

        foreach (var currentCode in role.PermissionCodes.Except(requestedCodes).ToList())
        {
            role.RevokePermission(currentCode);
        }

        foreach (var newCode in requestedCodes.Except(role.PermissionCodes))
        {
            var grantResult = role.GrantPermission(newCode);
            if (grantResult.IsFailure)
            {
                return grantResult;
            }
        }

        roleRepository.Update(role);

        return Result.Success();
    }
}
