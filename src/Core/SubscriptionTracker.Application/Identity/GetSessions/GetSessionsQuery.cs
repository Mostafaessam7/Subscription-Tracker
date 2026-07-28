using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.GetSessions;

public sealed record GetSessionsQuery : IQuery<IReadOnlyList<SessionDto>>;

public sealed record SessionDto(Guid Id, DateTimeOffset CreatedAtUtc, DateTimeOffset ExpiresAtUtc, string? CreatedByIp);
