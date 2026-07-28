using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Identity.Login;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Specifications;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Infrastructure.Security;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class LoginCommandHandlerTwoFactorTests
{
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly IRepository<Workspace, Guid> _workspaceRepository = Substitute.For<IRepository<Workspace, Guid>>();
    private readonly IRepository<Role, Guid> _roleRepository = Substitute.For<IRepository<Role, Guid>>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService _jwtTokenService = Substitute.For<IJwtTokenService>();
    private readonly TotpService _twoFactorService = new(TimeProvider.System);

    private readonly LoginCommandHandler _handler;

    public LoginCommandHandlerTwoFactorTests()
    {
        _passwordHasher.Verify(Arg.Any<string>(), Arg.Any<string>()).Returns(true);
        _workspaceRepository.ListAsync(Arg.Any<Specification<Workspace>>(), Arg.Any<CancellationToken>())
            .Returns(new List<Workspace>());

        _handler = new LoginCommandHandler(
            _userRepository, _workspaceRepository, _roleRepository, _passwordHasher, _jwtTokenService,
            _twoFactorService, TimeProvider.System);
    }

    private User CreateUserWithTwoFactorEnabled(out string secret)
    {
        var user = User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;
        secret = _twoFactorService.GenerateSecret();
        user.EnableTwoFactor(secret);
        _userRepository.FirstOrDefaultAsync(Arg.Any<Specification<User>>(), Arg.Any<CancellationToken>()).Returns(user);
        return user;
    }

    private string FindValidCode(string secret)
    {
        for (var i = 0; i < 1_000_000; i++)
        {
            var candidate = i.ToString("D6", System.Globalization.CultureInfo.InvariantCulture);
            if (_twoFactorService.ValidateCode(secret, candidate))
            {
                return candidate;
            }
        }

        throw new InvalidOperationException("No valid TOTP code found - this should never happen.");
    }

    [Fact]
    public async Task Handle_WithTwoFactorEnabledAndNoCode_ShouldRequireCodeWithoutRecordingFailedLogin()
    {
        var user = CreateUserWithTwoFactorEnabled(out _);

        var result = await _handler.Handle(new LoginCommand("jane@example.com", "correct-password", "127.0.0.1"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Login.TwoFactorRequired");
        user.FailedLoginAttempts.Should().Be(0);
    }

    [Fact]
    public async Task Handle_WithTwoFactorEnabledAndWrongCode_ShouldFailAndRecordFailedLogin()
    {
        var user = CreateUserWithTwoFactorEnabled(out _);

        var result = await _handler.Handle(
            new LoginCommand("jane@example.com", "correct-password", "127.0.0.1", "000000"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Login.InvalidTwoFactorCode");
        user.FailedLoginAttempts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_WithTwoFactorEnabledAndCorrectCode_ShouldSucceed()
    {
        var user = CreateUserWithTwoFactorEnabled(out var secret);
        _jwtTokenService.GenerateAccessToken(user, Arg.Any<Guid?>(), Arg.Any<IReadOnlyCollection<string>>())
            .Returns(new AccessTokenResult("token", DateTimeOffset.UtcNow.AddMinutes(15)));
        _jwtTokenService.GenerateRefreshToken().Returns("raw-refresh-token");
        _jwtTokenService.HashRefreshToken("raw-refresh-token").Returns("hashed");

        var result = await _handler.Handle(
            new LoginCommand("jane@example.com", "correct-password", "127.0.0.1", FindValidCode(secret)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.FailedLoginAttempts.Should().Be(0);
    }
}
