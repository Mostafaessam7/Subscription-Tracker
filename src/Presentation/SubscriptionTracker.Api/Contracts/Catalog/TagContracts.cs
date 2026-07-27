namespace SubscriptionTracker.Api.Contracts.Catalog;

public sealed record CreateTagRequest(string Name, string? Color);

public sealed record UpdateTagRequest(string Name, string? Color);
