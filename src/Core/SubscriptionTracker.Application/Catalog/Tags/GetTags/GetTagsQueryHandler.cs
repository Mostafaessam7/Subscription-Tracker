using Microsoft.EntityFrameworkCore;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.Tags.GetTags;

public sealed class GetTagsQueryHandler(IApplicationDbContext dbContext, ICurrentUserService currentUserService)
    : IQueryHandler<GetTagsQuery, IReadOnlyList<TagDto>>
{
    public async Task<Result<IReadOnlyList<TagDto>>> Handle(GetTagsQuery request, CancellationToken cancellationToken)
    {
        var tags = await dbContext.Tags
            .Where(t => t.WorkspaceId == currentUserService.WorkspaceId)
            .OrderBy(t => t.Name)
            .Select(TagProjections.ToDto)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<TagDto>>(tags);
    }
}
