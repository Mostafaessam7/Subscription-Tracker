using FluentValidation;

namespace SubscriptionTracker.Application.Subscriptions.GetSubscriptions;

public sealed class GetSubscriptionsQueryValidator : AbstractValidator<GetSubscriptionsQuery>
{
    public GetSubscriptionsQueryValidator()
    {
        RuleFor(q => q.PageNumber).GreaterThanOrEqualTo(1);
        RuleFor(q => q.PageSize).InclusiveBetween(1, 100);
    }
}
