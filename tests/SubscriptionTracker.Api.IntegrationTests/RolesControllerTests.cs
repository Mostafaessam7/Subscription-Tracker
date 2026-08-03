using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class RolesControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly string[] InitialPermissionCodes = ["budgets:view", "budgets:manage"];
    private static readonly string[] UpdatedPermissionCodes = ["budgets:view", "budgets:manage", "reports:view"];
    private readonly HttpClient _client;

    public RolesControllerTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUpdateDeleteRole_ShouldRoundTrip()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var createRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Post, "/api/v1/roles", session.AccessToken);
        createRequest.Content = JsonContent.Create(new
        {
            name = "Billing Manager",
            description = "Manages budgets",
            permissionCodes = InitialPermissionCodes,
        });
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var roleId = await createResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        using var listRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/roles", session.AccessToken);
        var listResponse = await _client.SendAsync(listRequest);
        var roles = await listResponse.Content.ReadFromJsonAsync<List<RoleDetailDto>>(JsonOptions);
        var created = roles.Should().ContainSingle(r => r.Id == roleId).Subject;
        created.Permissions.Should().BeEquivalentTo(["budgets:view", "budgets:manage"]);
        created.IsSystemRole.Should().BeFalse();

        using var updateRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Put, $"/api/v1/roles/{roleId}", session.AccessToken);
        updateRequest.Content = JsonContent.Create(new
        {
            name = "Billing Manager",
            description = "Manages budgets and views reports",
            permissionCodes = UpdatedPermissionCodes,
        });
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var deleteRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Delete, $"/api/v1/roles/{roleId}", session.AccessToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetPermissionCatalog_ShouldReturnKnownPermissionCodes()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var request = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/roles/permissions", session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var catalog = await response.Content.ReadFromJsonAsync<List<PermissionCatalogEntryDto>>(JsonOptions);
        catalog.Should().Contain(p => p.Code == "subscriptions:view");
    }

    private sealed record RoleDetailDto(Guid Id, string Name, string? Description, bool IsSystemRole, List<string> Permissions);
    private sealed record PermissionCatalogEntryDto(string Code, string Category);
}
