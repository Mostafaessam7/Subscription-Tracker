using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Application.Budgets.UpdateBudget;

public sealed class UpdateBudgetCommandHandler(IRepository<Budget, Guid> budgetRepository, ICurrentUserService currentUserService)
    : ICommandHandler<UpdateBudgetCommand>
{
    public async Task<Result> Handle(UpdateBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetRepository.GetByIdAsync(request.BudgetId, cancellationToken);
        if (budget is null || budget.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("UpdateBudget.NotFound", "Budget was not found."));
        }

        var amountResult = Money.Create(request.Amount, request.CurrencyCode);
        if (amountResult.IsFailure)
        {
            return Result.Failure(amountResult.Error);
        }

        budget.UpdateAmount(amountResult.Value);
        budget.UpdateThreshold(request.AlertThresholdPercentage);
        budgetRepository.Update(budget);

        return Result.Success();
    }
}
