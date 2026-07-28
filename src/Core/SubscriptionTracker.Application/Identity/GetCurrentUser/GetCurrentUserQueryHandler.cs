using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Identity.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetCurrentUserQuery, CurrentUserDto>
{
    public async Task<Result<CurrentUserDto>> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var user = await dbContext.Users
            .Where(u => u.Id == currentUserService.UserId)
            .Select(u => new CurrentUserDto(u.Id, u.Email.Value, u.FirstName, u.LastName, u.TwoFactorEnabled))
            .FirstOrDefaultAsync(cancellationToken);

        return user is null
            ? Result.Failure<CurrentUserDto>(Error.NotFound("GetCurrentUser.NotFound", "User was not found."))
            : user;
    }
}
