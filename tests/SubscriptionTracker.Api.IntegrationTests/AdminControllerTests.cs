using System.Net;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class AdminControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;
    private readonly HttpClient _client;

    public AdminControllerTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/v1/admin/users")]
    [InlineData("/api/v1/admin/workspaces")]
    [InlineData("/api/v1/admin/health")]
    public async Task AdminEndpoints_ForARegularUser_ShouldReturnForbidden(string path)
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var request = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, path, session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminEndpoints_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/admin/users");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task TriggerJob_ForARegularUser_ShouldReturnForbidden()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var request = TestAuthHelper.AuthorizedRequest(
            HttpMethod.Post, "/api/v1/admin/jobs/budget-alert/trigger", session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Theory]
    [InlineData("renewal-reminder")]
    [InlineData("auto-renewal")]
    [InlineData("expire-subscriptions")]
    [InlineData("budget-alert")]
    public async Task TriggerJob_ForASystemAdmin_WithAKnownJobName_ShouldReturnNoContent(string jobName)
    {
        var session = await TestAuthHelper.RegisterAndLoginAsSystemAdminAsync(_client, _factory);

        using var request = TestAuthHelper.AuthorizedRequest(
            HttpMethod.Post, $"/api/v1/admin/jobs/{jobName}/trigger", session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task TriggerJob_ForASystemAdmin_WithAnUnknownJobName_ShouldReturnNotFound()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsSystemAdminAsync(_client, _factory);

        using var request = TestAuthHelper.AuthorizedRequest(
            HttpMethod.Post, "/api/v1/admin/jobs/does-not-exist/trigger", session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
