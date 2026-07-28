using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Common;
using SubscriptionTracker.Domain.Tenancy;
using SubscriptionTracker.Domain.Tenancy.Specifications;

namespace SubscriptionTracker.Application.Tenancy.AcceptInvitation;

public sealed class AcceptInvitationCommandHandler(
    IRepository<Workspace, Guid> workspaceRepository, ICurrentUserService currentUserService, TimeProvider timeProvider)
    : ICommandHandler<AcceptInvitationCommand>
{
    public async Task<Result> Handle(AcceptInvitationCommand request, CancellationToken cancellationToken)
    {
        var workspace = await workspaceRepository.FirstOrDefaultAsync(
            new WorkspaceByMemberIdSpecification(request.MemberId), cancellationToken);

        if (workspace is null)
        {
            return Result.Failure(Error.NotFound("AcceptInvitation.NotFound", "Invitation was not found."));
        }

        var member = workspace.Members.First(m => m.Id == request.MemberId);
        if (member.UserId != currentUserService.UserId)
        {
            return Result.Failure(Error.Forbidden("AcceptInvitation.NotYourInvitation", "This invitation does not belong to you."));
        }

        var result = workspace.AcceptInvitation(request.MemberId, timeProvider.GetUtcNow());
        if (result.IsFailure)
        {
            return result;
        }

        workspaceRepository.Update(workspace);

        return Result.Success();
    }
}
