using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.Tags.UpdateTag;

public sealed record UpdateTagCommand(Guid TagId, string Name, string? Color) : ICommand;
