using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Domain.Tenancy.Specifications;

public sealed class EmailInvitationsByEmailSpecification : Specification<EmailInvitation>
{
    public EmailInvitationsByEmailSpecification(Email email)
    {
        AddCriteria(i => i.Email == email && i.ConsumedAtUtc == null);
    }
}
