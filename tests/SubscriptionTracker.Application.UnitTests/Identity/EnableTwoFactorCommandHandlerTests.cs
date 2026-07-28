using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Identity.EnableTwoFactor;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Infrastructure.Security;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class EnableTwoFactorCommandHandlerTests
{
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly TotpService _twoFactorService = new(TimeProvider.System);

    private readonly EnableTwoFactorCommandHandler _handler;

    public EnableTwoFactorCommandHandlerTests()
    {
        _handler = new EnableTwoFactorCommandHandler(_userRepository, _currentUserService, _twoFactorService);
    }

    private static User CreateUser() => User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;

    [Fact]
    public async Task Handle_WithCorrectCodeForTheSecret_ShouldEnableTwoFactorAndPersist()
    {
        var user = CreateUser();
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var secret = _twoFactorService.GenerateSecret();
        var code = GenerateCurrentCode(secret);

        var result = await _handler.Handle(new EnableTwoFactorCommand(secret, code), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeTrue();
        user.TwoFactorSecret.Should().Be(secret);
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WithWrongCode_ShouldFailAndNotEnable()
    {
        var user = CreateUser();
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var secret = _twoFactorService.GenerateSecret();

        var result = await _handler.Handle(new EnableTwoFactorCommand(secret, "000000"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("EnableTwoFactor.InvalidCode");
        user.TwoFactorEnabled.Should().BeFalse();
    }

    private string GenerateCurrentCode(string secret)
    {
        // No public "generate" API exists (only ValidateCode, since real callers are authenticator apps) -
        // brute-force the 6-digit space against the real service to find one it accepts for "now".
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
}
