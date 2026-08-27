using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Identity.Register;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class RegisterUserCommandHandlerTests
{
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly IRepository<Role, Guid> _roleRepository = Substitute.For<IRepository<Role, Guid>>();
    private readonly IRepository<Workspace, Guid> _workspaceRepository = Substitute.For<IRepository<Workspace, Guid>>();
    private readonly IRepository<EmailInvitation, Guid> _emailInvitationRepository = Substitute.For<IRepository<EmailInvitation, Guid>>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");
        _emailInvitationRepository
            .ListAsync(Arg.Any<Specification<EmailInvitation>>(), Arg.Any<CancellationToken>())
            .Returns([]);

        _handler = new RegisterUserCommandHandler(
            _userRepository, _roleRepository, _workspaceRepository, _emailInvitationRepository,
            _passwordHasher, _emailSender, TimeProvider.System);
    }

    private static RegisterUserCommand ValidCommand() => new("jane@example.com", "Str0ngPass!", "Jane", "Doe", null);

    [Fact]
    public async Task Handle_WithNewEmail_ShouldSucceedAndPersistAggregates()
    {
        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepository.Received(1).Add(Arg.Any<User>());
        _roleRepository.Received(1).Add(Arg.Any<Role>());
        _workspaceRepository.Received(1).Add(Arg.Any<Workspace>());
        await _emailSender.Received(1).SendEmailVerificationAsync(
            "jane@example.com", "Jane Doe", Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExistingEmail_ShouldSucceedWithoutCreatingADuplicateAccountAndNotifyTheOwner()
    {
        // Non-enumerable, same principle as ForgotPasswordCommandHandler: the API must not reveal whether an
        // email is already registered via a distinct error, so this reports the same success shape a real
        // registration would - no new User/Role/Workspace is created, and the existing account owner is
        // emailed instead so they aren't left confused by a verification email that never arrives.
        var existingUser = User.Register(
            Domain.Common.ValueObjects.Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;

        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
        _roleRepository.DidNotReceive().Add(Arg.Any<Role>());
        _workspaceRepository.DidNotReceive().Add(Arg.Any<Workspace>());
        await _emailSender.Received(1).SendDuplicateRegistrationAttemptAsync(
            "jane@example.com", "Jane Doe", Arg.Any<CancellationToken>());
        await _emailSender.DidNotReceive().SendEmailVerificationAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAPendingEmailInvitation_ShouldAutoJoinTheInvitingWorkspace()
    {
        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        var invitedWorkspaceId = Guid.NewGuid();
        var inviterRoleId = Guid.NewGuid();
        var invitedRole = Role.Create("Viewer", null, invitedWorkspaceId, isSystemRole: true).Value;
        var invitedWorkspace = Workspace.Create("Acme", Guid.NewGuid(), inviterRoleId, DateTimeOffset.UtcNow, invitedWorkspaceId).Value;

        var email = Domain.Common.ValueObjects.Email.Create("jane@example.com").Value;
        var invitation = EmailInvitation.Create(
            invitedWorkspaceId, email, invitedRole.Id, Guid.NewGuid(), "hash",
            DateTimeOffset.UtcNow, DateTimeOffset.UtcNow.AddDays(7));

        _emailInvitationRepository
            .ListAsync(Arg.Any<Specification<EmailInvitation>>(), Arg.Any<CancellationToken>())
            .Returns([invitation]);
        _workspaceRepository.GetByIdAsync(invitedWorkspaceId, Arg.Any<CancellationToken>()).Returns(invitedWorkspace);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        invitedWorkspace.Members.Should().Contain(m => m.RoleId == invitedRole.Id);
        invitation.IsConsumed.Should().BeTrue();
        _workspaceRepository.Received(1).Update(invitedWorkspace);
        _emailInvitationRepository.Received(1).Update(invitation);
    }
}
