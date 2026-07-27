using MediatR;
using Microsoft.Extensions.Logging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result
{
    public async Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;

        logger.LogInformation("Handling {RequestName}", requestName);

        var response = await next();

        if (response.IsFailure)
        {
            logger.LogWarning(
                "{RequestName} failed with error {ErrorCode}: {ErrorMessage}",
                requestName, response.Error.Code, response.Error.Message);
        }
        else
        {
            logger.LogInformation("Handled {RequestName} successfully", requestName);
        }

        return response;
    }
}
