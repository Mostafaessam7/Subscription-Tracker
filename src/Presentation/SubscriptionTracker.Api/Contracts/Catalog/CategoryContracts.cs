namespace SubscriptionTracker.Api.Contracts.Catalog;

public sealed record CreateCategoryRequest(string Name, string? Color, string? Icon);

public sealed record UpdateCategoryRequest(string Name, string? Color, string? Icon);
