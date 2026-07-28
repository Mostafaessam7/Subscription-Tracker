using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Tenancy;

namespace SubscriptionTracker.Application.Tenancy.UpdateWorkspaceSettings;

public sealed class UpdateWorkspaceSettingsCommandHandler(
    IRepository<Workspace, Guid> workspaceRepository, ICurrentUserService currentUserService)
    : ICommandHandler<UpdateWorkspaceSettingsCommand>
{
    public async Task<Result> Handle(UpdateWorkspaceSettingsCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure(
                Error.Unauthorized("UpdateWorkspaceSettings.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var workspace = await workspaceRepository.GetByIdAsync(currentUserService.WorkspaceId.Value, cancellationToken);
        if (workspace is null)
        {
            return Result.Failure(Error.NotFound("UpdateWorkspaceSettings.NotFound", "Workspace was not found."));
        }

        var settingsResult = WorkspaceSettings.Create(request.DefaultCurrencyCode, request.TimeZoneId, request.Locale);
        if (settingsResult.IsFailure)
        {
            return Result.Failure(settingsResult.Error);
        }

        workspace.UpdateSettings(settingsResult.Value);
        workspaceRepository.Update(workspace);

        return Result.Success();
    }
}
