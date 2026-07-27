using MediatR;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Common.Messaging;

public interface IQuery<TResponse> : IRequest<Result<TResponse>>;
