namespace SubscriptionTracker.Application.Abstractions;

/// <summary>TOTP (RFC 6238) secret generation, authenticator-app provisioning URIs, and code validation.</summary>
public interface ITwoFactorService
{
    /// <summary>Generates a new random Base32-encoded secret suitable for an authenticator app.</summary>
    string GenerateSecret();

    /// <summary>Builds an otpauth:// URI (renderable as a QR code) for the given secret, account, and issuer.</summary>
    string GetProvisioningUri(string secret, string accountEmail, string issuer);

    /// <summary>Validates a 6-digit code against the secret, tolerating +/-1 time step of clock drift.</summary>
    bool ValidateCode(string secret, string code);
}
