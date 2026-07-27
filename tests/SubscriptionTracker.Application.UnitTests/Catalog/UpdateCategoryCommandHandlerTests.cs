using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Catalog.Categories.UpdateCategory;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.UnitTests.Catalog;

public class UpdateCategoryCommandHandlerTests
{
    private readonly IRepository<Category, Guid> _categoryRepository = Substitute.For<IRepository<Category, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly UpdateCategoryCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public UpdateCategoryCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new UpdateCategoryCommandHandler(_categoryRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WhenCategoryNotFound_ShouldFailWithNotFound()
    {
        _categoryRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((Category?)null);

        var result = await _handler.Handle(
            new UpdateCategoryCommand(Guid.NewGuid(), "Streaming", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UpdateCategory.NotFound");
    }

    [Fact]
    public async Task Handle_WhenCategoryBelongsToAnotherWorkspace_ShouldFailWithNotFound()
    {
        var category = Category.Create(Guid.NewGuid(), "Streaming").Value;
        _categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);

        var result = await _handler.Handle(
            new UpdateCategoryCommand(category.Id, "Renamed", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UpdateCategory.NotFound");
    }

    [Fact]
    public async Task Handle_WhenRenamingToAnotherCategorysName_ShouldFailWithConflict()
    {
        var category = Category.Create(_workspaceId, "Streaming").Value;
        var other = Category.Create(_workspaceId, "Utilities").Value;

        _categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<Specification<Category>>(), Arg.Any<CancellationToken>())
            .Returns(other);

        var result = await _handler.Handle(
            new UpdateCategoryCommand(category.Id, "Utilities", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UpdateCategory.DuplicateName");
    }

    [Fact]
    public async Task Handle_WithValidRename_ShouldSucceedAndPersist()
    {
        var category = Category.Create(_workspaceId, "Streaming").Value;
        _categoryRepository.GetByIdAsync(category.Id, Arg.Any<CancellationToken>()).Returns(category);
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<Specification<Category>>(), Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var result = await _handler.Handle(
            new UpdateCategoryCommand(category.Id, "Renamed", "#00FF00", "movie"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        category.Name.Should().Be("Renamed");
        category.Color.Should().Be("#00FF00");
        _categoryRepository.Received(1).Update(category);
    }
}
