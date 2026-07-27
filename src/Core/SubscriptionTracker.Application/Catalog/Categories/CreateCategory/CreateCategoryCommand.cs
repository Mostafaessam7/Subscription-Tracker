using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.Categories.CreateCategory;

public sealed record CreateCategoryCommand(string Name, string? Color, string? Icon) : ICommand<Guid>;
