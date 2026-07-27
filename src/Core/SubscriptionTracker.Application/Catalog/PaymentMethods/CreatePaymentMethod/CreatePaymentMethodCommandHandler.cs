using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Catalog.Specifications;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.CreatePaymentMethod;

public sealed class CreatePaymentMethodCommandHandler(
    IRepository<PaymentMethod, Guid> paymentMethodRepository, ICurrentUserService currentUserService)
    : ICommandHandler<CreatePaymentMethodCommand, Guid>
{
    public async Task<Result<Guid>> Handle(CreatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        if (currentUserService.WorkspaceId is null)
        {
            return Result.Failure<Guid>(
                Error.Unauthorized("CreatePaymentMethod.NoActiveWorkspace", "You must be signed in with an active workspace."));
        }

        var workspaceId = currentUserService.WorkspaceId.Value;

        var paymentMethodResult = PaymentMethod.Create(
            workspaceId, request.Type, request.Label, request.MaskedDetails, request.IsDefault);

        if (paymentMethodResult.IsFailure)
        {
            return Result.Failure<Guid>(paymentMethodResult.Error);
        }

        var paymentMethod = paymentMethodResult.Value;

        if (request.IsDefault)
        {
            await UnmarkOtherDefaultsAsync(paymentMethodRepository, workspaceId, cancellationToken);
        }

        paymentMethodRepository.Add(paymentMethod);

        return Result.Success(paymentMethod.Id);
    }

    internal static async Task UnmarkOtherDefaultsAsync(
        IRepository<PaymentMethod, Guid> repository, Guid workspaceId, CancellationToken cancellationToken, Guid? exceptId = null)
    {
        var currentDefaults = await repository.ListAsync(
            new DefaultPaymentMethodByWorkspaceSpecification(workspaceId), cancellationToken);

        foreach (var paymentMethod in currentDefaults)
        {
            if (paymentMethod.Id == exceptId)
            {
                continue;
            }

            paymentMethod.UnmarkAsDefault();
            repository.Update(paymentMethod);
        }
    }
}
