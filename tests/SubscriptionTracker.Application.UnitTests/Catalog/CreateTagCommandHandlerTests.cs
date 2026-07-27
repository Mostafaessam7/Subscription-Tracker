using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Catalog.Tags.CreateTag;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.UnitTests.Catalog;

public class CreateTagCommandHandlerTests
{
    private readonly IRepository<Tag, Guid> _tagRepository = Substitute.For<IRepository<Tag, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly CreateTagCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public CreateTagCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new CreateTagCommandHandler(_tagRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WithNewName_ShouldSucceedAndPersist()
    {
        _tagRepository.FirstOrDefaultAsync(Arg.Any<Specification<Tag>>(), Arg.Any<CancellationToken>())
            .Returns((Tag?)null);

        var result = await _handler.Handle(new CreateTagCommand("Essential", "#0000FF"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _tagRepository.Received(1).Add(Arg.Any<Tag>());
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ShouldFailWithConflict()
    {
        var existing = Tag.Create(_workspaceId, "Essential").Value;
        _tagRepository.FirstOrDefaultAsync(Arg.Any<Specification<Tag>>(), Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await _handler.Handle(new CreateTagCommand("Essential", null), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreateTag.DuplicateName");
    }
}
