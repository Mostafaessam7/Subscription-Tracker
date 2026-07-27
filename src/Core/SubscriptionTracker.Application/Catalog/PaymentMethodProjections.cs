using System.Linq.Expressions;
using SubscriptionTracker.Domain.Catalog;

namespace SubscriptionTracker.Application.Catalog;

internal static class PaymentMethodProjections
{
    public static readonly Expression<Func<PaymentMethod, PaymentMethodDto>> ToDto = p =>
        new PaymentMethodDto(p.Id, p.Type, p.Label, p.MaskedDetails, p.IsDefault);
}
