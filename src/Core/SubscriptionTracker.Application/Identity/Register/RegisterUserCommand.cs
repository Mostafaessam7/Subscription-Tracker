using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.Register;

public sealed record RegisterUserCommand(
    string Email, string Password, string FirstName, string LastName, string? WorkspaceName) : ICommand<RegisterUserResponse>;

public sealed record RegisterUserResponse(Guid UserId, Guid WorkspaceId);
