using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Common.ValueObjects;

namespace SubscriptionTracker.Domain.Tenancy;

/// <summary>
/// A pending invitation to join a workspace, addressed to an email that has no registered account yet (unlike
/// WorkspaceMember, which requires an existing UserId). Consumed automatically at registration time if the new
/// user's email matches - see RegisterUserCommandHandler.
/// </summary>
public sealed class EmailInvitation : AggregateRoot<Guid>
{
    private EmailInvitation(
        Guid id, Guid workspaceId, Email email, Guid roleId, Guid invitedByUserId, string tokenHash,
        DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc)
        : base(id)
    {
        WorkspaceId = workspaceId;
        Email = email;
        RoleId = roleId;
        InvitedByUserId = invitedByUserId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    private EmailInvitation()
    {
    }

    public Guid WorkspaceId { get; private set; }
    public Email Email { get; private set; } = null!;
    public Guid RoleId { get; private set; }
    public Guid InvitedByUserId { get; private set; }
    public string TokenHash { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset ExpiresAtUtc { get; private set; }
    public DateTimeOffset? ConsumedAtUtc { get; private set; }

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAtUtc;
    public bool IsConsumed => ConsumedAtUtc is not null;
    public bool IsValid => !IsExpired && !IsConsumed;

    public static EmailInvitation Create(
        Guid workspaceId, Email email, Guid roleId, Guid invitedByUserId, string tokenHash,
        DateTimeOffset createdAtUtc, DateTimeOffset expiresAtUtc) =>
        new(Guid.NewGuid(), workspaceId, email, roleId, invitedByUserId, tokenHash, createdAtUtc, expiresAtUtc);

    public void Consume() => ConsumedAtUtc = DateTimeOffset.UtcNow;
}
