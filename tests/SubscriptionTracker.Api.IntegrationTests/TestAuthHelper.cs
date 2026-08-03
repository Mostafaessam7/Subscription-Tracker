using System.Net.Http.Json;
using System.Text.Json;

namespace SubscriptionTracker.Api.IntegrationTests;

public sealed record LoginResponseDto(Guid UserId, string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken, Guid? WorkspaceId);

public sealed record AuthenticatedSession(string Email, string AccessToken, Guid WorkspaceId, Guid UserId);

/// <summary>Shared register+login boilerplate for integration tests that need an authenticated client.</summary>
public static class TestAuthHelper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private const string Password = "Str0ngPass!123";

    public static async Task<AuthenticatedSession> RegisterAndLoginAsync(HttpClient client, string? workspaceName = null)
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var registerResponse = await client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = Password,
            firstName = "Test",
            lastName = "User",
            workspaceName,
        });
        registerResponse.EnsureSuccessStatusCode();

        var loginResponse = await client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = Password });
        loginResponse.EnsureSuccessStatusCode();

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

        return new AuthenticatedSession(email, login!.AccessToken, login.WorkspaceId!.Value, login.UserId);
    }

    public static HttpRequestMessage AuthorizedRequest(HttpMethod method, string requestUri, string accessToken) =>
        new(method, requestUri) { Headers = { Authorization = new("Bearer", accessToken) } };
}
