using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.RevokeSession;

public sealed record RevokeSessionCommand(Guid RefreshTokenId, string? RevokedByIp) : ICommand;
