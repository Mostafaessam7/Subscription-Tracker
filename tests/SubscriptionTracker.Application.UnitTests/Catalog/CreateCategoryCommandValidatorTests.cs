using FluentAssertions;
using SubscriptionTracker.Application.Catalog.Categories.CreateCategory;

namespace SubscriptionTracker.Application.UnitTests.Catalog;

public class CreateCategoryCommandValidatorTests
{
    private readonly CreateCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidCommand_ShouldPass()
    {
        var result = _validator.Validate(new CreateCategoryCommand("Streaming", "#FF0000", "tv"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldFail()
    {
        var result = _validator.Validate(new CreateCategoryCommand("", null, null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithNameTooLong_ShouldFail()
    {
        var result = _validator.Validate(new CreateCategoryCommand(new string('a', 101), null, null));

        result.IsValid.Should().BeFalse();
    }
}
