using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class AuthAndSubscriptionsFlowTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _client;

    public AuthAndSubscriptionsFlowTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthLive_ShouldReturnOk()
    {
        var response = await _client.GetAsync("/health/live");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task FullFlow_RegisterLoginCreateSubscriptionAndList_ShouldSucceed()
    {
        var email = $"{Guid.NewGuid():N}@example.com";

        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", new
        {
            email,
            password = "Str0ngPass!",
            firstName = "Jane",
            lastName = "Doe",
            workspaceName = (string?)null,
        });
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", new { email, password = "Str0ngPass!" });
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseDto>(JsonOptions);
        login.Should().NotBeNull();
        login!.AccessToken.Should().NotBeNullOrEmpty();
        login.WorkspaceId.Should().NotBeNull();

        using var authorizedRequest = new HttpRequestMessage(HttpMethod.Post, "/api/v1/subscriptions");
        authorizedRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);
        authorizedRequest.Content = JsonContent.Create(new
        {
            name = "Netflix",
            provider = "Netflix Inc.",
            logoUrl = (string?)null,
            websiteUrl = (string?)null,
            notes = (string?)null,
            categoryId = (Guid?)null,
            paymentMethodId = (Guid?)null,
            amount = 9.99m,
            currencyCode = "USD",
            billingFrequency = 1,
            customIntervalDays = (int?)null,
            startDate = "2026-01-01",
            trialEndDate = (string?)null,
            autoRenewal = true,
            tagIds = (Guid[]?)null,
        });

        var createResponse = await _client.SendAsync(authorizedRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        using var listRequest = new HttpRequestMessage(HttpMethod.Get, "/api/v1/subscriptions");
        listRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", login.AccessToken);

        var listResponse = await _client.SendAsync(listRequest);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var page = await listResponse.Content.ReadFromJsonAsync<PagedListDto>(JsonOptions);
        page.Should().NotBeNull();
        page!.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task Subscriptions_WithoutAuthentication_ShouldReturnUnauthorized()
    {
        var response = await _client.GetAsync("/api/v1/subscriptions");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    private sealed record LoginResponseDto(Guid UserId, string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc, string RefreshToken, Guid? WorkspaceId);

    private sealed record PagedListDto(int TotalCount, int PageNumber, int PageSize);
}
