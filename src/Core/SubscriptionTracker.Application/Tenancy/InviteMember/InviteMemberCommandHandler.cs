using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.InviteMember;

public sealed class InviteMemberCommandHandler(
    IRepository<Workspace, Guid> workspaceRepository,
    IRepository<User, Guid> userRepository,
    IRepository<Role, Guid> roleRepository,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : ICommandHandler<InviteMemberCommand, Guid>
{
    public async Task<Result<Guid>> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("InviteMember.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var invitedUser = await userRepository.FirstOrDefaultAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken);
        if (invitedUser is null)
        {
            return Result.Failure<Guid>(
                Error.NotFound("InviteMember.UserNotFound", "No registered user was found with this email address."));
        }

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null || (role.WorkspaceId is not null && role.WorkspaceId != currentUserService.WorkspaceId))
        {
            return Result.Failure<Guid>(Error.NotFound("InviteMember.RoleNotFound", "Role was not found."));
        }

        var workspace = await workspaceRepository.GetByIdAsync(currentUserService.WorkspaceId.Value, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure<Guid>(Error.NotFound("InviteMember.WorkspaceNotFound", "Workspace was not found."));
        }

        var inviteResult = workspace.InviteMember(invitedUser.Id, role.Id, timeProvider.GetUtcNow());
        if (inviteResult.IsFailure)
        {
            return Result.Failure<Guid>(inviteResult.Error);
        }

        workspaceRepository.Update(workspace);

        return Result.Success(inviteResult.Value.Id);
    }
}
