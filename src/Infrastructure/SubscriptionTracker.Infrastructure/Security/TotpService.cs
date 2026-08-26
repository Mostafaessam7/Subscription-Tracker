using System.Security.Cryptography;
using System.Text;
using SubscriptionTracker.Application.Abstractions;

namespace SubscriptionTracker.Infrastructure.Security;

/// <summary>
/// Hand-rolled RFC 6238 TOTP (30s step, 6 digits, SHA1 per the de-facto Google Authenticator/Microsoft
/// Authenticator convention - RFC 6238 permits other hash algorithms, but authenticator apps overwhelmingly
/// only support SHA1) plus RFC 4648 Base32 encode/decode for the secret. No external dependency: the primitives
/// involved (HMACSHA1 + a base32 alphabet) are small enough that pulling in a package wasn't worth it, matching
/// PasswordHasher's hand-rolled PBKDF2 elsewhere in this file.
/// </summary>
public sealed class TotpService(TimeProvider timeProvider) : ITwoFactorService
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
    private const int SecretLengthBytes = 20;
    private const int CodeDigits = 6;
    private static readonly TimeSpan TimeStep = TimeSpan.FromSeconds(30);

    // Crockford-style alphabet: no 0/O, 1/I/L, or other easily-confused characters - these get hand-copied
    // or read aloud by a locked-out user, unlike the TOTP secret which only ever gets scanned as a QR code.
    private const string RecoveryCodeAlphabet = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
    private const int RecoveryCodeGroupLength = 5;

    public string GenerateSecret()
    {
        var secretBytes = RandomNumberGenerator.GetBytes(SecretLengthBytes);
        return Base32Encode(secretBytes);
    }

    public string GetProvisioningUri(string secret, string accountEmail, string issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedLabel = Uri.EscapeDataString($"{issuer}:{accountEmail}");
        return $"otpauth://totp/{encodedLabel}?secret={secret}&issuer={encodedIssuer}&algorithm=SHA1&digits={CodeDigits}&period=30";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits || !code.All(char.IsDigit))
        {
            return false;
        }

        var secretBytes = Base32Decode(secret);
        var currentStep = GetCurrentTimeStep();

        // Tolerate +/-1 step (30s) of clock drift between the server and the user's device.
        for (var drift = -1; drift <= 1; drift++)
        {
            if (ComputeCode(secretBytes, currentStep + drift) == code)
            {
                return true;
            }
        }

        return false;
    }

    public IReadOnlyList<string> GenerateRecoveryCodes(int count)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var groupA = RandomAlphabetString(RecoveryCodeGroupLength);
            var groupB = RandomAlphabetString(RecoveryCodeGroupLength);
            codes.Add($"{groupA}-{groupB}");
        }

        return codes;
    }

    private static string RandomAlphabetString(int length)
    {
        var chars = new char[length];
        for (var i = 0; i < length; i++)
        {
            chars[i] = RecoveryCodeAlphabet[RandomNumberGenerator.GetInt32(RecoveryCodeAlphabet.Length)];
        }

        return new string(chars);
    }

    private long GetCurrentTimeStep() => timeProvider.GetUtcNow().ToUnixTimeSeconds() / (long)TimeStep.TotalSeconds;

    private static string ComputeCode(byte[] secretBytes, long timeStep)
    {
        var timeStepBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timeStepBytes);
        }

        // HMAC-SHA1 is mandated by RFC 6238/RFC 4226 for TOTP and is what every authenticator app (Google/Microsoft
        // Authenticator, 1Password, etc.) implements - it is used here purely as a keyed PRF, not for collision
        // resistance, so SHA1's known weaknesses (which are about collision attacks) do not apply to this use.
#pragma warning disable CA5350
        using var hmac = new HMACSHA1(secretBytes);
#pragma warning restore CA5350
        var hash = hmac.ComputeHash(timeStepBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        var truncated = binaryCode % (int)Math.Pow(10, CodeDigits);
        return truncated.ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(CodeDigits, '0');
    }

    private static string Base32Encode(byte[] data)
    {
        var builder = new StringBuilder((data.Length * 8 + 4) / 5);
        var buffer = data[0];
        var bitsLeft = 8;
        var index = 1;

        while (bitsLeft > 0 || index < data.Length)
        {
            if (bitsLeft < 5)
            {
                if (index < data.Length)
                {
                    buffer = (byte)((buffer << 8) | data[index]);
                    bitsLeft += 8;
                    index++;
                }
                else
                {
                    buffer <<= 5 - bitsLeft;
                    bitsLeft = 5;
                }
            }

            bitsLeft -= 5;
            builder.Append(Base32Alphabet[(buffer >> bitsLeft) & 0x1F]);
        }

        return builder.ToString();
    }

    private static byte[] Base32Decode(string base32)
    {
        var cleaned = base32.Trim().TrimEnd('=').ToUpperInvariant();
        var bytes = new List<byte>(cleaned.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var c in cleaned)
        {
            var value = Base32Alphabet.IndexOf(c);
            if (value < 0)
            {
                continue;
            }

            buffer = (buffer << 5) | value;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                bytes.Add((byte)((buffer >> bitsLeft) & 0xFF));
            }
        }

        return bytes.ToArray();
    }
}
