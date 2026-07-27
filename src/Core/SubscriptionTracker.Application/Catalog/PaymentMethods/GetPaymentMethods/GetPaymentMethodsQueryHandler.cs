using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.GetPaymentMethods;

public sealed class GetPaymentMethodsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetPaymentMethodsQuery, IReadOnlyList<PaymentMethodDto>>
{
    public async Task<Result<IReadOnlyList<PaymentMethodDto>>> Handle(GetPaymentMethodsQuery request, CancellationToken cancellationToken)
    {
        var paymentMethods = await dbContext.PaymentMethods
            .Where(p => p.WorkspaceId == currentUserService.WorkspaceId)
            .OrderBy(p => p.Label)
            .Select(PaymentMethodProjections.ToDto)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PaymentMethodDto>>(paymentMethods);
    }
}
