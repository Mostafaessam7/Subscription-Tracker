using MediatR;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Common.Messaging;

public interface ICommand : IRequest<Result>, IBaseCommand;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>, IBaseCommand;

public interface IBaseCommand;
