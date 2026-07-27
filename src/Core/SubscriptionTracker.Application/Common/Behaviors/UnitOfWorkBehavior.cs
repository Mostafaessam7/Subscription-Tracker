using MediatR;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Common.Behaviors;

/// <summary>
/// Persists changes after a command handler returns, whether the business Result is a success or a failure -
/// handlers may legitimately mutate state before returning a failure (e.g. recording a failed login attempt).
/// An unhandled exception still skips the save entirely, since control never reaches this point. Queries never
/// reach this behavior.
/// </summary>
public sealed class UnitOfWorkBehavior<TRequest, TResponse>(IUnitOfWork unitOfWork) : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IBaseCommand)
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return response;
    }
}
