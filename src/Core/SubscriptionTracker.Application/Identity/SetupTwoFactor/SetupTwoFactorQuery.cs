using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Identity.SetupTwoFactor;

public sealed record SetupTwoFactorQuery : IQuery<SetupTwoFactorResponse>;

public sealed record SetupTwoFactorResponse(string Secret, string ProvisioningUri);
