using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.EnableTwoFactor;

public sealed record EnableTwoFactorCommand(string Secret, string Code) : ICommand<EnableTwoFactorResponse>;

/// <summary>
/// <see cref="RecoveryCodes"/> is returned exactly once, in the clear - only their hashes are persisted (see
/// <see cref="SubscriptionTracker.Domain.Identity.TwoFactorRecoveryCode"/>). The frontend must show these to
/// the user immediately and cannot fetch them again later.
/// </summary>
public sealed record EnableTwoFactorResponse(IReadOnlyList<string> RecoveryCodes);
