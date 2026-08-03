using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Administration.GetAllUsers;

/// <summary>System-admin only: every registered user across every tenant.</summary>
public sealed record GetAllUsersQuery : IQuery<IReadOnlyList<AdminUserSummaryDto>>;
