using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Budgets.CreateBudget;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.UnitTests.Budgets;

public class CreateBudgetCommandHandlerTests
{
    private readonly IRepository<Budget, Guid> _budgetRepository = Substitute.For<IRepository<Budget, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly CreateBudgetCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public CreateBudgetCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new CreateBudgetCommandHandler(_budgetRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WithValidCommand_ShouldSucceedAndPersist()
    {
        var command = new CreateBudgetCommand("Streaming", 100m, "USD", BudgetPeriod.Monthly, null, 80);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _budgetRepository.Received(1).Add(Arg.Any<Budget>());
    }

    [Fact]
    public async Task Handle_WithNoActiveWorkspace_ShouldFailWithUnauthorized()
    {
        _currentUserService.WorkspaceId.Returns((Guid?)null);
        var command = new CreateBudgetCommand("Streaming", 100m, "USD", BudgetPeriod.Monthly, null, 80);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("CreateBudget.NoActiveWorkspace");
    }

    [Fact]
    public async Task Handle_WithInvalidCurrencyCode_ShouldFail()
    {
        var command = new CreateBudgetCommand("Streaming", 100m, "US", BudgetPeriod.Monthly, null, 80);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        _budgetRepository.DidNotReceive().Add(Arg.Any<Budget>());
    }
}
