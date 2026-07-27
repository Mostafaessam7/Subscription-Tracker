using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.Categories.GetCategories;

public sealed record GetCategoriesQuery : IQuery<IReadOnlyList<CategoryDto>>;
