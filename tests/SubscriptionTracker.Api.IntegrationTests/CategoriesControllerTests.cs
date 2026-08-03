using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class CategoriesControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public CategoriesControllerTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateUpdateDeleteCategory_ShouldRoundTrip()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var createRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Post, "/api/v1/categories", session.AccessToken);
        createRequest.Content = JsonContent.Create(new { name = "Streaming", color = "#FF0000", icon = (string?)null });
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var categoryId = await createResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        using var duplicateRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Post, "/api/v1/categories", session.AccessToken);
        duplicateRequest.Content = JsonContent.Create(new { name = "Streaming", color = (string?)null, icon = (string?)null });
        var duplicateResponse = await _client.SendAsync(duplicateRequest);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        using var updateRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Put, $"/api/v1/categories/{categoryId}", session.AccessToken);
        updateRequest.Content = JsonContent.Create(new { name = "Streaming Services", color = "#00FF00", icon = (string?)null });
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var listRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/categories", session.AccessToken);
        var listResponse = await _client.SendAsync(listRequest);
        var categories = await listResponse.Content.ReadFromJsonAsync<List<CategoryDto>>(JsonOptions);
        categories.Should().ContainSingle(c => c.Id == categoryId && c.Name == "Streaming Services");

        using var deleteRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Delete, $"/api/v1/categories/{categoryId}", session.AccessToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record CategoryDto(Guid Id, string Name);
}
