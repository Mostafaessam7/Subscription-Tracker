using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Identity;

namespace SubscriptionTracker.Application.Tenancy.Roles.GetPermissionCatalog;

public sealed class GetPermissionCatalogQueryHandler : IQueryHandler<GetPermissionCatalogQuery, IReadOnlyList<PermissionCatalogEntryDto>>
{
    public Task<Result<IReadOnlyList<PermissionCatalogEntryDto>>> Handle(
        GetPermissionCatalogQuery request, CancellationToken cancellationToken)
    {
        var entries = Permissions.All
            .Select(code => new PermissionCatalogEntryDto(code, code[..code.IndexOf(':')]))
            .ToList();

        return Task.FromResult(Result.Success<IReadOnlyList<PermissionCatalogEntryDto>>(entries));
    }
}
