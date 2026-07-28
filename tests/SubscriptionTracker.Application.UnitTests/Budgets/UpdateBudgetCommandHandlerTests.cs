using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Budgets.UpdateBudget;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Application.UnitTests.Budgets;

public class UpdateBudgetCommandHandlerTests
{
    private readonly IRepository<Budget, Guid> _budgetRepository = Substitute.For<IRepository<Budget, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly UpdateBudgetCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public UpdateBudgetCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new UpdateBudgetCommandHandler(_budgetRepository, _currentUserService);
    }

    private Budget CreateBudget() =>
        Budget.Create(_workspaceId, "Streaming", Money.Create(100m, "USD").Value, BudgetPeriod.Monthly, null, 80).Value;

    [Fact]
    public async Task Handle_WithValidCommand_ShouldUpdateAmountAndThreshold()
    {
        var budget = CreateBudget();
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);

        var result = await _handler.Handle(new UpdateBudgetCommand(budget.Id, 150m, "USD", 90), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        budget.Amount.Amount.Should().Be(150m);
        budget.AlertThresholdPercentage.Should().Be(90);
        _budgetRepository.Received(1).Update(budget);
    }

    [Fact]
    public async Task Handle_WhenBudgetBelongsToAnotherWorkspace_ShouldFailWithNotFound()
    {
        var budget = Budget.Create(Guid.NewGuid(), "Streaming", Money.Create(100m, "USD").Value, BudgetPeriod.Monthly).Value;
        _budgetRepository.GetByIdAsync(budget.Id, Arg.Any<CancellationToken>()).Returns(budget);

        var result = await _handler.Handle(new UpdateBudgetCommand(budget.Id, 150m, "USD", 90), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UpdateBudget.NotFound");
    }
}
