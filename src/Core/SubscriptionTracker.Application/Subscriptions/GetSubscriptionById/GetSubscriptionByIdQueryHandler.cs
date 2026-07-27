using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Subscriptions.GetSubscriptionById;

public sealed class GetSubscriptionByIdQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetSubscriptionByIdQuery, SubscriptionDto>
{
    public async Task<Result<SubscriptionDto>> Handle(GetSubscriptionByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await dbContext.Subscriptions
            .Where(s => s.Id == request.SubscriptionId && s.WorkspaceId == currentUserService.WorkspaceId)
            .Select(SubscriptionProjections.ToDto)
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result.Failure<SubscriptionDto>(Error.NotFound("GetSubscriptionById.NotFound", "Subscription was not found."))
            : dto;
    }
}
