using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Security;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Domain.Tenancy.Specifications;

namespace SubscriptionTracker.Application.Identity.Register;

public sealed class RegisterUserCommandHandler(
    IRepository<User, Guid> userRepository,
    IRepository<Role, Guid> roleRepository,
    IRepository<Workspace, Guid> workspaceRepository,
    IRepository<EmailInvitation, Guid> emailInvitationRepository,
    IPasswordHasher passwordHasher,
    IEmailSender emailSender,
    TimeProvider timeProvider)
    : ICommandHandler<RegisterUserCommand, RegisterUserResponse>
{
    private static readonly TimeSpan EmailVerificationTokenLifetime = TimeSpan.FromHours(24);

    public async Task<Result<RegisterUserResponse>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(emailResult.Error);
        }

        var existingUser = await userRepository.FirstOrDefaultAsync(new UserByEmailSpecification(emailResult.Value), cancellationToken);
        if (existingUser is not null)
        {
            // Non-enumerable, same principle as ForgotPasswordCommandHandler: revealing "this email is already
            // registered" via a distinct 409 lets anyone probe which emails have accounts. Report the exact same
            // success shape as a real registration (the frontend never reads RegisterUserResponse's fields - it
            // only checks for success and shows a generic "check your email" message) without actually creating
            // a duplicate account, and let the real owner know via email instead of leaving them silently
            // confused about a verification email that will never arrive. Fresh random Guids, not Guid.Empty -
            // an all-zero id would itself be a distinguishable signal to anyone comparing response bodies across
            // repeated probes, which would quietly defeat the entire point of this branch.
            await emailSender.SendDuplicateRegistrationAttemptAsync(existingUser.Email.Value, existingUser.FullName, cancellationToken);
            return new RegisterUserResponse(Guid.NewGuid(), Guid.NewGuid());
        }

        var passwordHash = passwordHasher.Hash(request.Password);

        var userResult = User.Register(emailResult.Value, passwordHash, request.FirstName, request.LastName);
        if (userResult.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(userResult.Error);
        }

        var user = userResult.Value;

        var now = timeProvider.GetUtcNow();
        var workspaceId = Guid.NewGuid();
        var workspaceName = string.IsNullOrWhiteSpace(request.WorkspaceName)
            ? $"{request.FirstName}'s Workspace"
            : request.WorkspaceName!;

        var roleResult = Role.Create("Owner", "Full access to this workspace", workspaceId);
        if (roleResult.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(roleResult.Error);
        }

        var role = roleResult.Value;
        foreach (var permission in Permissions.All)
        {
            role.GrantPermission(permission);
        }

        var workspaceResult = Workspace.Create(workspaceName, user.Id, role.Id, now, workspaceId);
        if (workspaceResult.IsFailure)
        {
            return Result.Failure<RegisterUserResponse>(workspaceResult.Error);
        }

        var rawVerificationToken = SecureTokenGenerator.Generate();
        user.IssueVerificationToken(
            VerificationTokenPurpose.EmailVerification,
            SecureTokenGenerator.Hash(rawVerificationToken),
            now.Add(EmailVerificationTokenLifetime));

        userRepository.Add(user);
        roleRepository.Add(role);
        workspaceRepository.Add(workspaceResult.Value);

        await ConsumeMatchingEmailInvitationsAsync(user, emailResult.Value, now, cancellationToken);

        await emailSender.SendEmailVerificationAsync(user.Email.Value, user.FullName, user.Id, rawVerificationToken, cancellationToken);

        return new RegisterUserResponse(user.Id, workspaceResult.Value.Id);
    }

    /// <summary>
    /// Auto-joins the new account to every workspace that invited this email address before they had an account
    /// (see InviteMemberCommandHandler's EmailInvitation branch) - added as an Invited member, same as the
    /// existing-user invite flow, so they still see and explicitly accept it via the pending-invitations panel.
    /// </summary>
    private async Task ConsumeMatchingEmailInvitationsAsync(
        User newUser, Email email, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var invitations = await emailInvitationRepository.ListAsync(new EmailInvitationsByEmailSpecification(email), cancellationToken);

        foreach (var invitation in invitations.Where(i => i.IsValid))
        {
            var invitedWorkspace = await workspaceRepository.GetByIdAsync(invitation.WorkspaceId, cancellationToken);
            if (invitedWorkspace is null)
            {
                continue;
            }

            var inviteResult = invitedWorkspace.InviteMember(newUser.Id, invitation.RoleId, now);
            if (inviteResult.IsFailure)
            {
                continue;
            }

            invitation.Consume();
            workspaceRepository.Update(invitedWorkspace);
            emailInvitationRepository.Update(invitation);
        }
    }
}
