using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class DashboardControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public DashboardControllerTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetSummary_ForFreshWorkspace_ShouldReturnZeroedSummary()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var request = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/dashboard/summary", session.AccessToken);
        var response = await _client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        summary.Should().NotBeNull();
        summary!.TotalSubscriptions.Should().Be(0);
        summary.ActiveCount.Should().Be(0);
        summary.TrialCount.Should().Be(0);
        summary.EstimatedMonthlySpend.Should().Be(0m);
        summary.UpcomingRenewals.Should().BeEmpty();
        summary.SpendByFrequency.Should().BeEmpty();
    }

    [Fact]
    public async Task GetSummary_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/dashboard/summary");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record DashboardSummaryDto(
        int TotalSubscriptions,
        int ActiveCount,
        int TrialCount,
        decimal EstimatedMonthlySpend,
        List<object> UpcomingRenewals,
        List<object> SpendByFrequency);
}
