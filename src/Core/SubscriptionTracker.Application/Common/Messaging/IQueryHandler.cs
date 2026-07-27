using MediatR;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Common.Messaging;

public interface IQueryHandler<in TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>;
