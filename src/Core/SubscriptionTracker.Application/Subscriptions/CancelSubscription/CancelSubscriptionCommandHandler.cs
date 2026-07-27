using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.CancelSubscription;

public sealed class CancelSubscriptionCommandHandler(
    IRepository<Subscription, Guid> subscriptionRepository, ICurrentUserService currentUserService)
    : ICommandHandler<CancelSubscriptionCommand>
{
    public async Task<Result> Handle(CancelSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("CancelSubscription.NotFound", "Subscription was not found."));
        }

        var result = subscription.Cancel(request.EffectiveDate, request.Reason);
        if (result.IsFailure)
        {
            return result;
        }

        subscriptionRepository.Update(subscription);
        return Result.Success();
    }
}
