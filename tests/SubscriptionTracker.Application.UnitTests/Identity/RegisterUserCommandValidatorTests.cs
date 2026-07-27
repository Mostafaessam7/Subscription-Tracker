using FluentAssertions;
using SubscriptionTracker.Application.Identity.Register;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class RegisterUserCommandValidatorTests
{
    private readonly RegisterUserCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var command = new RegisterUserCommand("jane@example.com", "Str0ngPass!", "Jane", "Doe", null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        var command = new RegisterUserCommand(email, "Str0ngPass!", "Jane", "Doe", null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("short1A")]
    [InlineData("nouppercase1")]
    [InlineData("NOLOWERCASE1")]
    [InlineData("NoDigitsHere")]
    public void Validate_WithWeakPassword_ShouldFail(string password)
    {
        var command = new RegisterUserCommand("jane@example.com", password, "Jane", "Doe", null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithEmptyFirstName_ShouldFail()
    {
        var command = new RegisterUserCommand("jane@example.com", "Str0ngPass!", "", "Doe", null);

        var result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }
}
