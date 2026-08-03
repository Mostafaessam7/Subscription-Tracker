using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Administration.DisableUser;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.UnitTests.Administration;

public class DisableUserCommandHandlerTests
{
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly DisableUserCommandHandler _handler;
    private readonly Guid _adminId = Guid.NewGuid();

    public DisableUserCommandHandlerTests()
    {
        _currentUserService.UserId.Returns(_adminId);
        _handler = new DisableUserCommandHandler(_userRepository, _currentUserService);
    }

    private static User CreateUser() => User.Register(Email.Create($"{Guid.NewGuid():N}@example.com").Value, "hash", "Jane", "Doe").Value;

    [Fact]
    public async Task Handle_ShouldDisableTargetUser()
    {
        var target = CreateUser();
        _userRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);

        var result = await _handler.Handle(new DisableUserCommand(target.Id), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.Status.Should().Be(UserStatus.Disabled);
        _userRepository.Received(1).Update(target);
    }

    [Fact]
    public async Task Handle_WhenTargetIsSelf_ShouldFail()
    {
        var result = await _handler.Handle(new DisableUserCommand(_adminId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DisableUser.CannotDisableSelf");
        _ = _userRepository.DidNotReceive().GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ForUnknownUser_ShouldFailWithNotFound()
    {
        var missingId = Guid.NewGuid();
        _userRepository.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await _handler.Handle(new DisableUserCommand(missingId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DisableUser.NotFound");
    }
}
