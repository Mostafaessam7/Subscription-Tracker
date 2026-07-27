using SubscriptionTracker.Application.Common.Messaging;

namespace SubscriptionTracker.Application.Catalog.Tags.DeleteTag;

public sealed record DeleteTagCommand(Guid TagId) : ICommand;
