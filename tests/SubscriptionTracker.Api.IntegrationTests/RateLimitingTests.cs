using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

/// <summary>Own fixture instance (not shared with AuthAndSubscriptionsFlowTests) so this test's rate-limiter
/// state - keyed by client IP, which TestServer's in-memory pipeline reports as null/"anonymous" for every
/// request - doesn't bleed into or get bled into by unrelated tests hitting the same policy.</summary>
public class RateLimitingTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public RateLimitingTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ForgotPassword_BeyondTheAuthSensitiveLimit_ShouldReturnTooManyRequests()
    {
        HttpResponseMessage? lastResponse = null;

        // The auth-sensitive policy permits 5 requests per 15-minute window; the 6th must be rejected.
        for (var i = 0; i < 6; i++)
        {
            lastResponse = await _client.PostAsJsonAsync(
                "/api/v1/auth/forgot-password", new { email = $"probe{i}@example.com" });
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Login_BeyondTheAuthLoginLimit_ShouldReturnTooManyRequests()
    {
        HttpResponseMessage? lastResponse = null;

        // The auth-login policy permits 30 requests per 1-minute window; the 31st must be rejected. Deliberately
        // a wrong-password guess against a nonexistent account (401, not 429) below the limit, then push past it -
        // proves the throttle applies before any single account could ever hit its own 5-attempt lockout.
        for (var i = 0; i < 31; i++)
        {
            lastResponse = await _client.PostAsJsonAsync(
                "/api/v1/auth/login", new { email = $"probe{i}@example.com", password = "wrong-password" });
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }

    [Fact]
    public async Task Register_BeyondTheAuthRegisterLimit_ShouldReturnTooManyRequests()
    {
        HttpResponseMessage? lastResponse = null;

        // The auth-register policy permits 30 requests per 1-minute window; the 31st must be rejected.
        for (var i = 0; i < 31; i++)
        {
            lastResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
            {
                email = $"probe{i}-{Guid.NewGuid():N}@example.com",
                password = "Str0ngPass!123",
                firstName = "Probe",
                lastName = "User",
            });
        }

        lastResponse!.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
    }
}
