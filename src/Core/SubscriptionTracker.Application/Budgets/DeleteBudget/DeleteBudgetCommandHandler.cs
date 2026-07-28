using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Budgets;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Budgets.DeleteBudget;

public sealed class DeleteBudgetCommandHandler(IRepository<Budget, Guid> budgetRepository, ICurrentUserService currentUserService)
    : ICommandHandler<DeleteBudgetCommand>
{
    public async Task<Result> Handle(DeleteBudgetCommand request, CancellationToken cancellationToken)
    {
        var budget = await budgetRepository.GetByIdAsync(request.BudgetId, cancellationToken);
        if (budget is null || budget.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("DeleteBudget.NotFound", "Budget was not found."));
        }

        budgetRepository.Remove(budget);

        return Result.Success();
    }
}
