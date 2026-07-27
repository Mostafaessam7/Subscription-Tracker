using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Catalog.PaymentMethods.CreatePaymentMethod;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.UpdatePaymentMethod;

public sealed class UpdatePaymentMethodCommandHandler(
    IRepository<PaymentMethod, Guid> paymentMethodRepository, ICurrentUserService currentUserService)
    : ICommandHandler<UpdatePaymentMethodCommand>
{
    public async Task<Result> Handle(UpdatePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethod = await paymentMethodRepository.GetByIdAsync(request.PaymentMethodId, cancellationToken);
        if (paymentMethod is null || paymentMethod.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("UpdatePaymentMethod.NotFound", "Payment method was not found."));
        }

        var renameResult = paymentMethod.Rename(request.Label);
        if (renameResult.IsFailure)
        {
            return renameResult;
        }

        if (request.IsDefault)
        {
            await CreatePaymentMethodCommandHandler.UnmarkOtherDefaultsAsync(
                paymentMethodRepository, paymentMethod.WorkspaceId, cancellationToken, paymentMethod.Id);
            paymentMethod.MarkAsDefault();
        }
        else
        {
            paymentMethod.UnmarkAsDefault();
        }

        paymentMethodRepository.Update(paymentMethod);

        return Result.Success();
    }
}
