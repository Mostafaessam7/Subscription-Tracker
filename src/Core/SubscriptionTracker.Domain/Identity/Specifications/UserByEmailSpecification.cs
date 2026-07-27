using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Domain.Identity.Specifications;

public sealed class UserByEmailSpecification : Specification<User>
{
    public UserByEmailSpecification(Email email)
    {
        AddCriteria(u => u.Email == email);
    }
}
