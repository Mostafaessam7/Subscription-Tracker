using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.UpdateSubscription;

public sealed class UpdateSubscriptionCommandHandler(
    IRepository<Subscription, Guid> subscriptionRepository, ICurrentUserService currentUserService)
    : ICommandHandler<UpdateSubscriptionCommand>
{
    public async Task<Result> Handle(UpdateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        var subscription = await subscriptionRepository.GetByIdAsync(request.SubscriptionId, cancellationToken);
        if (subscription is null || subscription.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("UpdateSubscription.NotFound", "Subscription was not found."));
        }

        var priceResult = Money.Create(request.Amount, request.CurrencyCode);
        if (priceResult.IsFailure)
        {
            return Result.Failure(priceResult.Error);
        }

        subscription.UpdateDetails(request.Name, request.Provider, request.LogoUrl, request.WebsiteUrl, request.Notes);
        subscription.UpdatePricing(priceResult.Value);
        subscription.ChangeCategory(request.CategoryId);
        subscription.ChangePaymentMethod(request.PaymentMethodId);

        foreach (var tagId in subscription.TagIds.ToList())
        {
            subscription.RemoveTag(tagId);
        }

        foreach (var tagId in request.TagIds ?? [])
        {
            subscription.AddTag(tagId);
        }

        subscriptionRepository.Update(subscription);
        return Result.Success();
    }
}
