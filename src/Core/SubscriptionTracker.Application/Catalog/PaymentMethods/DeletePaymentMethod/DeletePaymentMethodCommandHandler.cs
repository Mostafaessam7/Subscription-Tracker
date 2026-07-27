using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Common.Messaging;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.Catalog.PaymentMethods.DeletePaymentMethod;

public sealed class DeletePaymentMethodCommandHandler(
    IRepository<PaymentMethod, Guid> paymentMethodRepository, ICurrentUserService currentUserService)
    : ICommandHandler<DeletePaymentMethodCommand>
{
    public async Task<Result> Handle(DeletePaymentMethodCommand request, CancellationToken cancellationToken)
    {
        var paymentMethod = await paymentMethodRepository.GetByIdAsync(request.PaymentMethodId, cancellationToken);
        if (paymentMethod is null || paymentMethod.WorkspaceId != currentUserService.WorkspaceId)
        {
            return Result.Failure(Error.NotFound("DeletePaymentMethod.NotFound", "Payment method was not found."));
        }

        paymentMethodRepository.Remove(paymentMethod);

        return Result.Success();
    }
}
