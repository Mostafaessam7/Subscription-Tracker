using System.Security.Cryptography;
using System.Text;

namespace SubscriptionTracker.Application.Common.Security;

/// <summary>Generates and hashes high-entropy tokens for email verification, password reset, and refresh token storage.</summary>
public static class SecureTokenGenerator
{
    public static string Generate(int byteLength = 32) => Convert.ToBase64String(RandomNumberGenerator.GetBytes(byteLength))
        .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string Hash(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hash);
    }
}
