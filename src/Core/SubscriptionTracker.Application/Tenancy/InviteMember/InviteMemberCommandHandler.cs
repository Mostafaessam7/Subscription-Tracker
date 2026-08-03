using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Security;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Domain.Tenancy.Specifications;

namespace SubscriptionTracker.Application.Tenancy.InviteMember;

public sealed class InviteMemberCommandHandler(
    IRepository<Workspace, Guid> workspaceRepository,
    IRepository<User, Guid> userRepository,
    IRepository<Role, Guid> roleRepository,
    IRepository<EmailInvitation, Guid> emailInvitationRepository,
    IEmailSender emailSender,
    ICurrentUserService currentUserService,
    TimeProvider timeProvider)
    : ICommandHandler<InviteMemberCommand, Guid>
{
    private static readonly TimeSpan EmailInvitationLifetime = TimeSpan.FromDays(7);

    public async Task<Result<Guid>> Handle(InviteMemberCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("InviteMember.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var workspaceId = currentUserService.WorkspaceId.Value;

        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<Guid>(emailResult.Error);
        }

        var role = await roleRepository.GetByIdAsync(request.RoleId, cancellationToken);
        if (role is null || (role.WorkspaceId is not null && role.WorkspaceId != workspaceId))
        {
            return Result.Failure<Guid>(Error.NotFound("InviteMember.RoleNotFound", "Role was not found."));
        }

        var workspace = await workspaceRepository.GetByIdAsync(workspaceId, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure<Guid>(Error.NotFound("InviteMember.WorkspaceNotFound", "Workspace was not found."));
        }

        var invitedUser = await userRepository.FirstOrDefaultAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken);
        if (invitedUser is not null)
        {
            var inviteResult = workspace.InviteMember(invitedUser.Id, role.Id, timeProvider.GetUtcNow());
            if (inviteResult.IsFailure)
            {
                return Result.Failure<Guid>(inviteResult.Error);
            }

            workspaceRepository.Update(workspace);

            return Result.Success(inviteResult.Value.Id);
        }

        // No registered account yet - park the invitation as an EmailInvitation and email a sign-up link.
        // RegisterUserCommandHandler consumes any matching invitations the moment an account with this email
        // is created, adding them as a workspace member automatically.
        var existingInvitations = await emailInvitationRepository.ListAsync(
            new EmailInvitationsByEmailSpecification(emailResult.Value), cancellationToken);

        if (existingInvitations.Any(i => i.WorkspaceId == workspaceId && i.IsValid))
        {
            return Result.Failure<Guid>(
                Error.Conflict("InviteMember.AlreadyInvited", "This email already has a pending invitation to this workspace."));
        }

        var now = timeProvider.GetUtcNow();
        var rawToken = SecureTokenGenerator.Generate();
        var emailInvitation = EmailInvitation.Create(
            workspaceId, emailResult.Value, role.Id, currentUserService.UserId!.Value,
            SecureTokenGenerator.Hash(rawToken), now, now.Add(EmailInvitationLifetime));

        emailInvitationRepository.Add(emailInvitation);

        await emailSender.SendWorkspaceInvitationAsync(
            emailResult.Value.Value, workspace.Name, currentUserService.Email ?? "A workspace admin", rawToken, cancellationToken);

        return Result.Success(emailInvitation.Id);
    }
}
