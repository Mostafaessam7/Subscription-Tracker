using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Application.Budgets.CreateBudget;

public sealed class CreateBudgetCommandHandler(IRepository<Budget, Guid> budgetRepository, ICurrentUserService currentUserService)
    : ICommandHandler<CreateBudgetCommand, Guid>
{
    public Task<Result<Guid>> Handle(CreateBudgetCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Task.FromResult(Result.Failure<Guid>(
                Error.Unauthorized("CreateBudget.NoActiveWorkspace", "You must be signed in with an active workspace.")));
        }

        var amountResult = Money.Create(request.Amount, request.CurrencyCode);
        if (amountResult.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(amountResult.Error));
        }

        var budgetResult = Budget.Create(
            currentUserService.WorkspaceId.Value, request.Name, amountResult.Value, request.Period,
            request.CategoryId, request.AlertThresholdPercentage);

        if (budgetResult.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(budgetResult.Error));
        }

        budgetRepository.Add(budgetResult.Value);

        return Task.FromResult(Result.Success(budgetResult.Value.Id));
    }
}
