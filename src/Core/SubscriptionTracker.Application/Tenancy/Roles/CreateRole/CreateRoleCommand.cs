using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.Roles.CreateRole;

public sealed record CreateRoleCommand(string Name, string? Description, IReadOnlyCollection<string> PermissionCodes) : ICommand<Guid>;
