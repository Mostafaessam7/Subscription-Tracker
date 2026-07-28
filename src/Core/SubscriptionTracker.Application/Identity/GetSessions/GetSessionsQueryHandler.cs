using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Identity.GetSessions;

public sealed class GetSessionsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetSessionsQuery, IReadOnlyList<SessionDto>>
{
    public async Task<Result<IReadOnlyList<SessionDto>>> Handle(GetSessionsQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users.FirstOrDefaultAsync(u => u.Id == currentUserService.UserId, cancellationToken);
        if (user is null)
        {
            return Result.Success<IReadOnlyList<SessionDto>>([]);
        }

        var sessions = user.RefreshTokens
            .Where(t => t.IsActive)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => new SessionDto(t.Id, t.CreatedAtUtc, t.ExpiresAtUtc, t.CreatedByIp))
            .ToList();

        return Result.Success<IReadOnlyList<SessionDto>>(sessions);
    }
}
