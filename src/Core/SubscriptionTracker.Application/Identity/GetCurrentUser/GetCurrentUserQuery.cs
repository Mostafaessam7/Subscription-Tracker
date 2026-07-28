using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.GetCurrentUser;

public sealed record GetCurrentUserQuery : IQuery<CurrentUserDto>;

public sealed record CurrentUserDto(Guid Id, string Email, string FirstName, string LastName, bool TwoFactorEnabled);
