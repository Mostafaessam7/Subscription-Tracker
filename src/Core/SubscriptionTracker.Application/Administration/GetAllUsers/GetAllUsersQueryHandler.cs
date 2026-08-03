using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Administration.GetAllUsers;

public sealed class GetAllUsersQueryHandler(IApplicationDbContext dbContext) : IQueryHandler<GetAllUsersQuery, IReadOnlyList<AdminUserSummaryDto>>
{
    public async Task<Result<IReadOnlyList<AdminUserSummaryDto>>> Handle(GetAllUsersQuery request, CancellationToken cancellationToken)
    {
        var users = await dbContext.Users
            .OrderByDescending(u => u.CreatedAtUtc)
            .Select(u => new AdminUserSummaryDto(
                u.Id, u.Email.Value, u.FirstName, u.LastName, u.Status.ToString(),
                u.IsSystemAdmin, u.IsEmailVerified, u.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AdminUserSummaryDto>>(users);
    }
}
