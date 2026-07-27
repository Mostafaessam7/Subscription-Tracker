using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.Categories.UpdateCategory;

public sealed record UpdateCategoryCommand(Guid CategoryId, string Name, string? Color, string? Icon) : ICommand;
