using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.ResumeSubscription;

public sealed class ResumeSubscriptionCommandHandler(
    IRepository<Subscription, Guid> subscriptionRepository, ICurrentUserService currentUserService)
    : ICommandHandler<ResumeSubscriptionCommand>
{
    public async Task<Result> Handle(ResumeSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("ResumeSubscription.NotFound", "Subscription was not found."));
        }

        var result = subscription.Resume();
        if (result.IsFailure)
        {
            return result;
        }

        subscriptionRepository.Update(subscription);
        return Result.Success();
    }
}
