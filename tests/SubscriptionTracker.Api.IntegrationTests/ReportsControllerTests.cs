using System.Net;
using System.Net.Http.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class ReportsControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly HttpClient _client;

    public ReportsControllerTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Theory]
    [InlineData("/api/v1/reports/subscriptions/csv", "text/csv")]
    [InlineData("/api/v1/reports/subscriptions/excel", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("/api/v1/reports/subscriptions/pdf", "application/pdf")]
    public async Task ExportEndpoints_ShouldReturnTheirDeclaredContentType(string path, string expectedContentType)
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var createRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Post, "/api/v1/subscriptions", session.AccessToken);
        createRequest.Content = JsonContent.Create(new
        {
            name = "Netflix",
            provider = "Netflix Inc.",
            logoUrl = (string?)null,
            websiteUrl = (string?)null,
            notes = (string?)null,
            categoryId = (Guid?)null,
            paymentMethodId = (Guid?)null,
            amount = 15.99m,
            currencyCode = "USD",
            billingFrequency = 1,
            customIntervalDays = (int?)null,
            startDate = "2026-01-01",
            trialEndDate = (string?)null,
            autoRenewal = true,
            tagIds = (Guid[]?)null,
        });
        (await _client.SendAsync(createRequest)).StatusCode.Should().Be(HttpStatusCode.Created);

        using var exportRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, path, session.AccessToken);
        var exportResponse = await _client.SendAsync(exportRequest);

        exportResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        exportResponse.Content.Headers.ContentType!.MediaType.Should().Be(expectedContentType);

        var bytes = await exportResponse.Content.ReadAsByteArrayAsync();
        bytes.Should().NotBeEmpty();
    }
}
