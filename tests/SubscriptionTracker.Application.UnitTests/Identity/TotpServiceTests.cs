using System.Security.Cryptography;
using FluentAssertions;
using SubscriptionTracker.Infrastructure.Security;

namespace SubscriptionTracker.Application.UnitTests.Identity;

public class TotpServiceTests
{
    private static readonly DateTimeOffset FixedInstant = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider _timeProvider = new(FixedInstant);
    private readonly TotpService _totpService;

    public TotpServiceTests()
    {
        _totpService = new TotpService(_timeProvider);
    }

    [Fact]
    public void GenerateSecret_ShouldReturnNonEmptyBase32String()
    {
        var secret = _totpService.GenerateSecret();

        secret.Should().NotBeNullOrWhiteSpace();
        secret.Should().MatchRegex("^[A-Z2-7]+$");
    }

    [Fact]
    public void GetProvisioningUri_ShouldContainSecretAndIssuer()
    {
        var secret = _totpService.GenerateSecret();

        var uri = _totpService.GetProvisioningUri(secret, "jane@example.com", "Subscription Tracker");

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain($"secret={secret}");
        uri.Should().Contain("issuer=Subscription%20Tracker");
    }

    [Fact]
    public void ValidateCode_WithCodeComputedIndependentlyForTheSameSecretAndInstant_ShouldReturnTrue()
    {
        var secret = _totpService.GenerateSecret();
        var expectedCode = ComputeReferenceCode(secret, FixedInstant);

        _totpService.ValidateCode(secret, expectedCode).Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_OneStepInThePast_ShouldStillBeAcceptedForClockDrift()
    {
        var secret = _totpService.GenerateSecret();
        var codeFromThirtySecondsAgo = ComputeReferenceCode(secret, FixedInstant.AddSeconds(-30));

        _totpService.ValidateCode(secret, codeFromThirtySecondsAgo).Should().BeTrue();
    }

    [Fact]
    public void ValidateCode_TwoStepsInThePast_ShouldBeRejected()
    {
        var secret = _totpService.GenerateSecret();
        var codeFromSixtySecondsAgo = ComputeReferenceCode(secret, FixedInstant.AddSeconds(-60));

        _totpService.ValidateCode(secret, codeFromSixtySecondsAgo).Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_WithWrongCode_ShouldReturnFalse()
    {
        var secret = _totpService.GenerateSecret();

        _totpService.ValidateCode(secret, "000000").Should().BeFalse();
    }

    [Fact]
    public void ValidateCode_WithNonNumericCode_ShouldReturnFalse()
    {
        var secret = _totpService.GenerateSecret();

        _totpService.ValidateCode(secret, "abcdef").Should().BeFalse();
    }

    /// <summary>
    /// Independent re-implementation of RFC 6238 (same algorithm TotpService uses internally) so the test isn't
    /// just calling the code under test to generate its own "expected" value.
    /// </summary>
    private static string ComputeReferenceCode(string base32Secret, DateTimeOffset instant)
    {
        var secretBytes = Base32Decode(base32Secret);
        var timeStep = instant.ToUnixTimeSeconds() / 30;
        var timeStepBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
        {
            Array.Reverse(timeStepBytes);
        }

#pragma warning disable CA5350
        using var hmac = new HMACSHA1(secretBytes);
#pragma warning restore CA5350
        var hash = hmac.ComputeHash(timeStepBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        return (binaryCode % 1_000_000).ToString(System.Globalization.CultureInfo.InvariantCulture).PadLeft(6, '0');
    }

    private static byte[] Base32Decode(string base32)
    {
        const string alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
        var bytes = new List<byte>(base32.Length * 5 / 8);
        var buffer = 0;
        var bitsLeft = 0;

        foreach (var c in base32.Trim().TrimEnd('=').ToUpperInvariant())
        {
            var value = alphabet.IndexOf(c);
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

    private sealed class FakeTimeProvider(DateTimeOffset instant) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => instant;
    }
}
