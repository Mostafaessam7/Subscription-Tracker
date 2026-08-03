using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class NotificationsControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public NotificationsControllerTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetMyNotifications_ForAFreshUser_ShouldBeEmpty()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var listRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/notifications", session.AccessToken);
        var listResponse = await _client.SendAsync(listRequest);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var page = await listResponse.Content.ReadFromJsonAsync<PagedListDto>(JsonOptions);
        page!.TotalCount.Should().Be(0);

        using var countRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/notifications/unread-count", session.AccessToken);
        var countResponse = await _client.SendAsync(countRequest);
        countResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var count = await countResponse.Content.ReadFromJsonAsync<int>(JsonOptions);
        count.Should().Be(0);
    }

    [Fact]
    public async Task MarkAllAsRead_WithNoNotifications_ShouldStillSucceed()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var request = TestAuthHelper.AuthorizedRequest(HttpMethod.Post, "/api/v1/notifications/read-all", session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    private sealed record PagedListDto(int TotalCount);
}
