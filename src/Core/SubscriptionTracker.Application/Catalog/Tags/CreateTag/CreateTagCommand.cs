using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.Tags.CreateTag;

public sealed record CreateTagCommand(string Name, string? Color) : ICommand<Guid>;
