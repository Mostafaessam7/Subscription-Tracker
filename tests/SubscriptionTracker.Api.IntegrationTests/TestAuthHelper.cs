using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SubscriptionTracker.Infrastructure.Persistence;

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

    /// <summary>Registers, promotes the new user to system admin directly via the DB (there's no API
    /// path to do this - see <c>SystemAdminSeeder</c>), then logs in again so the returned session's
    /// JWT actually carries the `system_admin` claim baked in at login time.</summary>
    public static async Task<AuthenticatedSession> RegisterAndLoginAsSystemAdminAsync(
        HttpClient client, ApiWebApplicationFactory factory, string? workspaceName = null)
    {
        var session = await RegisterAndLoginAsync(client, workspaceName);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var user = await dbContext.Users.SingleAsync(u => u.Id == session.UserId);
            user.GrantSystemAdmin();
            await dbContext.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login", new { email = session.Email, password = Password });
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);

        return session with { AccessToken = login!.AccessToken };
    }
}
