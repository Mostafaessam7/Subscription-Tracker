using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Catalog.Categories.CreateCategory;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.UnitTests.Catalog;

public class CreateCategoryCommandHandlerTests
{
    private readonly IRepository<Category, Guid> _categoryRepository = Substitute.For<IRepository<Category, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly CreateCategoryCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public CreateCategoryCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new CreateCategoryCommandHandler(_categoryRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WithNewName_ShouldSucceedAndPersist()
    {
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<Specification<Category>>(), Arg.Any<CancellationToken>())
            .Returns((Category?)null);

        var result = await _handler.Handle(new CreateCategoryCommand("Streaming", null, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _categoryRepository.Received(1).Add(Arg.Any<Category>());
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldFailWithConflict()
    {
        var existing = Category.Create(_workspaceId, "Streaming").Value;
        _categoryRepository.FirstOrDefaultAsync(Arg.Any<Specification<Category>>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(new CreateCategoryCommand("Streaming", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreateCategory.DuplicateName");
        _categoryRepository.DidNotReceive().Add(Arg.Any<Category>());
    }

    [Fact]
    public async Task Handle_WithNoActiveWorkspace_ShouldFailWithUnauthorized()
    {
        _currentUserService.WorkspaceId.Returns((Guid?)null);

        var result = await _handler.Handle(new CreateCategoryCommand("Streaming", null, null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreateCategory.NoActiveWorkspace");
    }
}
