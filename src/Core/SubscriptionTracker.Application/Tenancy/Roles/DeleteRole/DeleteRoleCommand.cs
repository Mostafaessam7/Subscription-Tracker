using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Tenancy.Roles.DeleteRole;

public sealed record DeleteRoleCommand(Guid RoleId) : ICommand;
