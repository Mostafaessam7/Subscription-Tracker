using Microsoft.Extensions.Configuration;

namespace SubscriptionTracker.Api.Startup;

/// <summary>
/// Fails fast rather than letting a Production deployment silently sign JWTs with the checked-in dev placeholder
/// key. Standard ASP.NET Core configuration layering already lets `Jwt:SigningKey` be supplied via a
/// `Jwt__SigningKey` environment variable (or any other configuration provider, e.g. a secret manager) with no
/// code change - this guard only verifies that *some* real value made it through by the time the app is about to
/// run in Production. Extracted from Program.cs so it's unit-testable without spinning up a full host.
/// </summary>
public static class ProductionSecretsGuard
{
    public const string DevPlaceholderSigningKey = "dev-only-signing-key-do-not-use-in-production-please-replace-me";

    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="isProduction"/> is true and no real signing key was configured.
    /// </exception>
    public static void EnsureJwtSigningKeyIsConfigured(IConfiguration configuration, bool isProduction)
    {
        if (!isProduction)
        {
            return;
        }

        var signingKey = configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(signingKey) || signingKey == DevPlaceholderSigningKey)
        {
            throw new InvalidOperationException(
                "Jwt:SigningKey is missing or still set to the development placeholder. Set a real secret via the " +
                "Jwt__SigningKey environment variable (or another configuration provider) before running in Production.");
        }
    }
}
