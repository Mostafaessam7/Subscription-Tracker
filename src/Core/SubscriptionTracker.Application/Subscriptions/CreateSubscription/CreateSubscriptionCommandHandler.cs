using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;
using SubscriptionTracker.Domain.Subscriptions;

namespace SubscriptionTracker.Application.Subscriptions.CreateSubscription;

public sealed class CreateSubscriptionCommandHandler(
    IRepository<Subscription, Guid> subscriptionRepository, ICurrentUserService currentUserService)
    : ICommandHandler<CreateSubscriptionCommand, Guid>
{
    public Task<Result<Guid>> Handle(CreateSubscriptionCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.UserId is null || currentUserService.WorkspaceId is null)
        {
            return Task.FromResult(Result.Failure<Guid>(
                Error.Unauthorized("CreateSubscription.NoActiveWorkspace", "You must be signed in with an active workspace.")));
        }

        var priceResult = Money.Create(request.Amount, request.CurrencyCode);
        if (priceResult.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(priceResult.Error));
        }

        var billingCycleResult = BillingCycle.Create(request.BillingFrequency, request.CustomIntervalDays);
        if (billingCycleResult.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(billingCycleResult.Error));
        }

        var subscriptionResult = Subscription.Create(
            currentUserService.WorkspaceId.Value,
            currentUserService.UserId.Value,
            request.Name,
            request.Provider,
            priceResult.Value,
            billingCycleResult.Value,
            request.StartDate,
            request.TrialEndDate,
            request.AutoRenewal);

        if (subscriptionResult.IsFailure)
        {
            return Task.FromResult(Result.Failure<Guid>(subscriptionResult.Error));
        }

        var subscription = subscriptionResult.Value;
        subscription.UpdateDetails(request.Name, request.Provider, request.LogoUrl, request.WebsiteUrl, request.Notes);
        subscription.ChangeCategory(request.CategoryId);
        subscription.ChangePaymentMethod(request.PaymentMethodId);

        foreach (var tagId in request.TagIds ?? [])
        {
            subscription.AddTag(tagId);
        }

        subscriptionRepository.Add(subscription);

        return Task.FromResult(Result.Success(subscription.Id));
    }
}
