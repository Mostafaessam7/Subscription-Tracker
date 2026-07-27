using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Identity.Specifications;

public sealed class UserByRefreshTokenHashSpecification : Specification<User>
{
    public UserByRefreshTokenHashSpecification(string tokenHash)
    {
        AddCriteria(u => u.RefreshTokens.Any(t => t.TokenHash == tokenHash));
    }
}
