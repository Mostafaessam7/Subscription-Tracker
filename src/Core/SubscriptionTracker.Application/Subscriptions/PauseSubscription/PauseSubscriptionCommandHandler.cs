using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.PauseSubscription;

public sealed class PauseSubscriptionCommandHandler(
    IRepository<Subscription, Guid> subscriptionRepository, ICurrentUserService currentUserService)
    : ICommandHandler<PauseSubscriptionCommand>
{
    public async Task<Result> Handle(PauseSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("PauseSubscription.NotFound", "Subscription was not found."));
        }

        var result = subscription.Pause();
        if (result.IsFailure)
        {
            return result;
        }

        subscriptionRepository.Update(subscription);
        return Result.Success();
    }
}
