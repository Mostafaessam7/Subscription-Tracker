using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Identity.DisableTwoFactor;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Infrastructure.Security;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class DisableTwoFactorCommandHandlerTests
{
    private readonly IRepository<User, Guid> _userRepository = Substitute.For<IRepository<User, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();
    private readonly TotpService _twoFactorService = new(TimeProvider.System);

    private readonly DisableTwoFactorCommandHandler _handler;

    public DisableTwoFactorCommandHandlerTests()
    {
        _handler = new DisableTwoFactorCommandHandler(_userRepository, _currentUserService, _twoFactorService);
    }

    private User CreateUserWithTwoFactorEnabled(out string secret)
    {
        var user = User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;
        secret = _twoFactorService.GenerateSecret();
        user.EnableTwoFactor(secret);
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
    public async Task Handle_WithCorrectCode_ShouldDisableAndPersist()
    {
        var user = CreateUserWithTwoFactorEnabled(out var secret);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new DisableTwoFactorCommand(FindValidCode(secret)), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        user.TwoFactorEnabled.Should().BeFalse();
        _userRepository.Received(1).Update(user);
    }

    [Fact]
    public async Task Handle_WhenNotEnabled_ShouldFail()
    {
        var user = User.Register(Email.Create("jane@example.com").Value, "hash", "Jane", "Doe").Value;
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new DisableTwoFactorCommand("123456"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DisableTwoFactor.NotEnabled");
    }

    [Fact]
    public async Task Handle_WithWrongCode_ShouldFailAndRemainEnabled()
    {
        var user = CreateUserWithTwoFactorEnabled(out _);
        _currentUserService.UserId.Returns(user.Id);
        _userRepository.GetByIdAsync(user.Id, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _handler.Handle(new DisableTwoFactorCommand("000000"), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("DisableTwoFactor.InvalidCode");
        user.TwoFactorEnabled.Should().BeTrue();
    }
}
