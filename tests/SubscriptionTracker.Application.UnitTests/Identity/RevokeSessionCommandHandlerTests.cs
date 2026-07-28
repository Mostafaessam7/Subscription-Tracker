using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Identity.RevokeSession;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class RevokeSessionCommandHandlerTests
{
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly RevokeSessionCommandHandler _handler;

    public RevokeSessionCommandHandlerTests()
    {
        _handler = new RevokeSessionCommandHandler(_userRepository, _currentUserService);
    }

    private static User CreateUser() => User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;

    [Fact]
    public async Task Handle_WithActiveSession_ShouldRevokeAndPersist()
    {
        var user = CreateUser();
        var token = user.IssueRefreshToken("token-hash", DateTimeOffset.UtcNow.AddDays(7), "127.0.0.1");

        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new RevokeSessionCommand(token.Id, "10.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.IsActive.Should().BeFalse();
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WithUnknownSessionId_ShouldFail()
    {
        var user = CreateUser();

        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new RevokeSessionCommand(Guid.NewGuid(), "10.0.0.1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("User.RefreshTokenNotFound");
    }

    [Fact]
    public async Task Handle_WithNoSignedInUser_ShouldFailWithUnauthorized()
    {
        _currentUserService.UserId.Returns((Guid?)null);

        var result = await _handler.Handle(new RevokeSessionCommand(Guid.NewGuid(), "10.0.0.1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RevokeSession.NotSignedIn");
    }
}
