using FluentAssertions;
using SubscriptionTracker.Application.Identity.Login;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class LoginCommandValidatorTests
{
    private readonly LoginCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(new LoginCommand("jane@example.com", "whatever", "127.0.0.1"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyPassword_ShouldFail()
    {
        var result = _validator.Validate(new LoginCommand("jane@example.com", "", "127.0.0.1"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithInvalidEmail_ShouldFail()
    {
        var result = _validator.Validate(new LoginCommand("not-an-email", "whatever", "127.0.0.1"));

        result.IsValid.Should().BeFalse();
    }
}
