using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;

namespace SubscriptionTracker.Api.IntegrationTests;

public class BudgetsControllerTests : IClassFixture<ApiWebApplicationFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly HttpClient _client;

    public BudgetsControllerTests(ApiWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateGetUpdateDeleteBudget_ShouldRoundTrip()
    {
        var session = await TestAuthHelper.RegisterAndLoginAsync(_client);

        using var createRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Post, "/api/v1/budgets", session.AccessToken);
        createRequest.Content = JsonContent.Create(new
        {
            name = "Streaming",
            amount = 50m,
            currencyCode = "USD",
            period = 0,
            categoryId = (Guid?)null,
            alertThresholdPercentage = 80,
        });
        var createResponse = await _client.SendAsync(createRequest);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var budgetId = await createResponse.Content.ReadFromJsonAsync<Guid>(JsonOptions);

        using var listRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/budgets", session.AccessToken);
        var listResponse = await _client.SendAsync(listRequest);
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var budgets = await listResponse.Content.ReadFromJsonAsync<List<BudgetDto>>(JsonOptions);
        budgets.Should().ContainSingle(b => b.Id == budgetId && b.Name == "Streaming");

        using var updateRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Put, $"/api/v1/budgets/{budgetId}", session.AccessToken);
        updateRequest.Content = JsonContent.Create(new { amount = 75m, currencyCode = "USD", alertThresholdPercentage = 90 });
        var updateResponse = await _client.SendAsync(updateRequest);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var deleteRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Delete, $"/api/v1/budgets/{budgetId}", session.AccessToken);
        var deleteResponse = await _client.SendAsync(deleteRequest);
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var listAfterDeleteRequest = TestAuthHelper.AuthorizedRequest(HttpMethod.Get, "/api/v1/budgets", session.AccessToken);
        var listAfterDeleteResponse = await _client.SendAsync(listAfterDeleteRequest);
        var budgetsAfterDelete = await listAfterDeleteResponse.Content.ReadFromJsonAsync<List<BudgetDto>>(JsonOptions);
        budgetsAfterDelete.Should().NotContain(b => b.Id == budgetId);
    }

    private sealed record BudgetDto(Guid Id, string Name);
}
