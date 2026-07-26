using FluentAssertions;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Identity;
using SubscriptionTracker.Domain.Identity.Events;

namespace SubscriptionTracker.Domain.UnitTests.Identity;

public class UserTests
{
    private static User CreateUser()
    {
        var email = Email.Create("jane.doe@example.com").Value;
        return User.Register(email, "hashed-password", "Jane", "Doe").Value;
    }

    [Fact]
    public void Register_ShouldStartPendingVerificationAndRaiseEvent()
    {
        var user = CreateUser();

        user.Status.Should().Be(UserStatus.PendingVerification);
        user.IsEmailVerified.Should().BeFalse();
        user.DomainEvents.Should().ContainSingle(e => e is UserRegistered);
    }

    [Fact]
    public void VerifyEmail_ShouldActivateUser()
    {
        var user = CreateUser();

        user.VerifyEmail();

        user.IsEmailVerified.Should().BeTrue();
        user.Status.Should().Be(UserStatus.Active);
        user.DomainEvents.Should().Contain(e => e is UserEmailVerified);
    }

    [Fact]
    public void RecordFailedLogin_BelowThreshold_ShouldNotLockAccount()
    {
        var user = CreateUser();

        for (var i = 0; i < 4; i++)
        {
            user.RecordFailedLogin(DateTimeOffset.UtcNow);
        }

        user.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public void RecordFailedLogin_AtThreshold_ShouldLockAccountAndRaiseEvent()
    {
        var user = CreateUser();

        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(DateTimeOffset.UtcNow);
        }

        user.IsLockedOut.Should().BeTrue();
        user.DomainEvents.Should().Contain(e => e is UserLockedOut);
    }

    [Fact]
    public void RecordSuccessfulLogin_ShouldResetFailedAttemptsAndUnlock()
    {
        var user = CreateUser();
        for (var i = 0; i < 5; i++)
        {
            user.RecordFailedLogin(DateTimeOffset.UtcNow);
        }

        user.RecordSuccessfulLogin(DateTimeOffset.UtcNow);

        user.FailedLoginAttempts.Should().Be(0);
        user.IsLockedOut.Should().BeFalse();
    }

    [Fact]
    public void IssueRefreshToken_ThenRevoke_ShouldMarkTokenInactive()
    {
        var user = CreateUser();
        var token = user.IssueRefreshToken("token-hash", DateTimeOffset.UtcNow.AddDays(7), "127.0.0.1");

        var result = user.RevokeRefreshToken(token.TokenHash, "127.0.0.1");

        result.IsSuccess.Should().BeTrue();
        token.IsActive.Should().BeFalse();
    }

    [Fact]
    public void RevokeRefreshToken_WhenNotFound_ShouldFail()
    {
        var user = CreateUser();

        var result = user.RevokeRefreshToken("unknown-hash", "127.0.0.1");

        result.IsFailure.Should().BeTrue();
    }
}
