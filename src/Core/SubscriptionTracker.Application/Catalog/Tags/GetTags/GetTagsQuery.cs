using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.Tags.GetTags;

public sealed record GetTagsQuery : IQuery<IReadOnlyList<TagDto>>;
