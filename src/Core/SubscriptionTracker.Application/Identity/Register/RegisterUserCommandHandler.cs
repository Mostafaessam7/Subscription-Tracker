using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Application.Common.Security;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Identity.Register;

public sealed class RegisterUserCommandHandler(
    IRepository<User, Guid> userRepository,
    IRepository<Role, Guid> roleRepository,
    IRepository<Workspace, Guid> workspaceRepository,
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
            return Result.Failure<RegisterUserResponse>(
                Error.Conflict("Register.EmailAlreadyExists", "An account with this email already exists."));
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

        await emailSender.SendEmailVerificationAsync(user.Email.Value, user.FullName, user.Id, rawVerificationToken, cancellationToken);

        return new RegisterUserResponse(user.Id, workspaceResult.Value.Id);
    }
}
