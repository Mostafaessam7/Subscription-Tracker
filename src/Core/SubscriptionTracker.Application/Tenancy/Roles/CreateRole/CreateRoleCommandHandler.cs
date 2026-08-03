using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Tenancy.Roles.CreateRole;

public sealed class CreateRoleCommandHandler(IRepository<Role, Guid> roleRepository, ICurrentUserService currentUserService)
    : ICommandHandler<CreateRoleCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("CreateRole.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var roleResult = Role.Create(request.Name, request.Description, currentUserService.WorkspaceId.Value);
        if (roleResult.IsFailure)
        {
            return Result.Failure<Guid>(roleResult.Error);
        }

        var role = roleResult.Value;
        foreach (var permissionCode in request.PermissionCodes.Distinct())
        {
            var grantResult = role.GrantPermission(permissionCode);
            if (grantResult.IsFailure)
            {
                return Result.Failure<Guid>(grantResult.Error);
            }
        }

        roleRepository.Add(role);

        return Result.Success(role.Id);
    }
}
