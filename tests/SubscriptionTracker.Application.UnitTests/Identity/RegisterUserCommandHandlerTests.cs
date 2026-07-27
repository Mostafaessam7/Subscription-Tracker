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
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();

    private readonly RegisterUserCommandHandler _handler;

    public RegisterUserCommandHandlerTests()
    {
        _passwordHasher.Hash(Arg.Any<string>()).Returns("hashed-password");

        _handler = new RegisterUserCommandHandler(
            _userRepository, _roleRepository, _workspaceRepository, _passwordHasher, _emailSender, TimeProvider.System);
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
    public async Task Handle_WithExistingEmail_ShouldFailWithConflict()
    {
        var existingUser = User.Register(
            Domain.Common.ValueObjects.Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;

        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>())
            .Returns(existingUser);

        var result = await _handler.Handle(ValidCommand(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Register.EmailAlreadyExists");
        _userRepository.DidNotReceive().Add(Arg.Any<User>());
    }
}
