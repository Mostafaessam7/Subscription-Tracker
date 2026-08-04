using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Administration.TriggerBackgroundJob;

public sealed class TriggerBackgroundJobCommandHandler(IBackgroundJobTrigger backgroundJobTrigger)
    : ICommandHandler<TriggerBackgroundJobCommand>
{
    public async Task<Result> Handle(TriggerBackgroundJobCommand request, CancellationToken cancellationToken)
    {
        var triggered = await backgroundJobTrigger.TriggerAsync(request.JobName, cancellationToken);
        if (!triggered)
        {
            return Result.Failure(Error.NotFound(
                "TriggerBackgroundJob.UnknownJob",
                $"No background job named '{request.JobName}' is registered. Known jobs: {string.Join(", ", backgroundJobTrigger.JobNames)}."));
        }

        return Result.Success();
    }
}
