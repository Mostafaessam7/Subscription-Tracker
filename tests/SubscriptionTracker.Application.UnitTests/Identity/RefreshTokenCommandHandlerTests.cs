using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Identity.RefreshToken;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class RefreshTokenCommandHandlerTests
{
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly IRepository<Workspace, Guid> _workspaceRepository = Substitute.For<IRepository<Workspace, Guid>>();
    private readonly IRepository<Role, Guid> _roleRepository = Substitute.For<IRepository<Role, Guid>>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly ILogger<RefreshTokenCommandHandler> _logger = Substitute.For<ILogger<RefreshTokenCommandHandler>>();

    private readonly RefreshTokenCommandHandler _handler;

    public RefreshTokenCommandHandlerTests()
    {
        _jwtTokenService.HashRefreshToken(Arg.Any<string>()).Returns(callInfo => $"hash:{callInfo.Arg<string>()}");
        _handler = new RefreshTokenCommandHandler(_userRepository, _workspaceRepository, _roleRepository, _jwtTokenService, TimeProvider.System, _logger);
    }

    private User CreateUserWithActiveToken(out string rawToken)
    {
        var user = User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;
        rawToken = "raw-refresh-token";
        user.IssueRefreshToken("hash:raw-refresh-token", DateTimeOffset.UtcNow.AddDays(30), "127.0.0.1");
        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    [Fact]
    public async Task Handle_WithActiveToken_ShouldRotateItAndSucceed()
    {
        var user = CreateUserWithActiveToken(out var rawToken);
        _jwtTokenService.GenerateAccessToken(user, Arg.Any<Guid?>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new AccessTokenResult("access-token", DateTimeOffset.UtcNow.AddMinutes(15)));
        _jwtTokenService.GenerateRefreshToken().Returns("new-raw-refresh-token");

        var result = await _handler.Handle(new RefreshTokenCommand(rawToken, null, "127.0.0.1"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.RefreshTokens.Single(t => t.TokenHash == "hash:raw-refresh-token").IsRevoked.Should().BeTrue();
        user.RefreshTokens.Should().ContainSingle(t => t.IsActive);
    }

    [Fact]
    public async Task Handle_WithExpiredButNeverRevokedToken_ShouldFailWithoutRevokingOtherTokens()
    {
        var user = User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;
        user.IssueRefreshToken("hash:expired", DateTimeOffset.UtcNow.AddDays(-1), "127.0.0.1");
        user.IssueRefreshToken("hash:other-active", DateTimeOffset.UtcNow.AddDays(30), "127.0.0.1");
        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new RefreshTokenCommand("expired", null, "127.0.0.1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RefreshToken.Invalid");
        user.RefreshTokens.Single(t => t.TokenHash == "hash:other-active").IsActive.Should().BeTrue();
        _userRepository.DidNotReceive().Update(Arg.Any<User>());
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedToken_ShouldTreatAsReuseAndRevokeEveryActiveToken()
    {
        // Simulates theft: the legitimate client already rotated this token away (it's revoked, replaced by
        // a newer one), but someone presents the old raw value again - the one signal available that the old
        // value may have leaked to a second party.
        var user = User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;
        user.IssueRefreshToken("hash:stolen", DateTimeOffset.UtcNow.AddDays(30), "127.0.0.1");
        user.RevokeRefreshToken("hash:stolen", "127.0.0.1");
        user.IssueRefreshToken("hash:unrelated-session", DateTimeOffset.UtcNow.AddDays(30), "10.0.0.9");
        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new RefreshTokenCommand("stolen", null, "203.0.113.1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("RefreshToken.Invalid");
        user.RefreshTokens.Should().OnlyContain(t => t.IsRevoked);
        _userRepository.Received(1).Update(user);
    }
}
