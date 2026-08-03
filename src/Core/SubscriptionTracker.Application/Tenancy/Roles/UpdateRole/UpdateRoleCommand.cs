using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.Roles.UpdateRole;

public sealed record UpdateRoleCommand(Guid RoleId, string Name, string? Description, IReadOnlyCollection<string> PermissionCodes) : ICommand;
