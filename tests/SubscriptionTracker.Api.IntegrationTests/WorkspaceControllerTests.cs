using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class WorkspaceControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public WorkspaceControllerTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMyWorkspace_ShouldReturnTheWorkspaceCreatedAtRegistration()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client, "Acme");

        using var request = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/workspace", session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspace = await response.Content.ReadFromJsonAsync<WorkspaceDto>(JsonOptions);
        workspace!.Id.Should().Be(session.WorkspaceId);
        workspace.Name.Should().Be("Acme");
    }

    [Fact]
    public async Task GetMyWorkspaces_ShouldListTheOwnedWorkspaceAsCurrent()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var request = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/workspace/my-workspaces", session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var workspaces = await response.Content.ReadFromJsonAsync<List<MyWorkspaceSummaryDto>>(JsonOptions);
        workspaces.Should().ContainSingle(w => w.Id == session.WorkspaceId && w.IsCurrent && w.IsOwner);
    }

    [Fact]
    public async Task InviteMember_WithUnregisteredEmail_ShouldSucceedAndBeRejectedOnDuplicate()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var rolesRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/workspace/assignable-roles", session.AccessToken);
        var rolesResponse = await _client.SendAsync(rolesRequest);
        var roles = await rolesResponse.Content.ReadFromJsonAsync<List<RoleSummaryDto>>(JsonOptions);
        var viewerRoleId = roles!.First(r => r.Name == "Viewer").Id;

        var invitedEmail = $"{Guid.NewGuid():N}@example.com";

        using var inviteRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Post, "/api/v1/workspace/members", session.AccessToken);
        inviteRequest.Content = JsonContent.Create(new { email = invitedEmail, roleId = viewerRoleId });
        var inviteResponse = await _client.SendAsync(inviteRequest);
        inviteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var duplicateInviteRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Post, "/api/v1/workspace/members", session.AccessToken);
        duplicateInviteRequest.Content = JsonContent.Create(new { email = invitedEmail, roleId = viewerRoleId });
        var duplicateInviteResponse = await _client.SendAsync(duplicateInviteRequest);
        duplicateInviteResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task UpdateSettings_ShouldPersist()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var updateRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Put, "/api/v1/workspace/settings", session.AccessToken);
        updateRequest.Content = JsonContent.Create(new { defaultCurrencyCode = "EUR", timeZoneId = "Europe/Berlin", locale = "de-DE" });
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var getRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/workspace", session.AccessToken);
        var getResponse = await _client.SendAsync(getRequest);
        var workspace = await getResponse.Content.ReadFromJsonAsync<WorkspaceDto>(JsonOptions);
        workspace!.DefaultCurrencyCode.Should().Be("EUR");
    }

    private sealed record WorkspaceDto(Guid Id, string Name, string DefaultCurrencyCode);
    private sealed record MyWorkspaceSummaryDto(Guid Id, string Name, string RoleName, bool IsOwner, bool IsCurrent);
    private sealed record RoleSummaryDto(Guid Id, string Name);
}
