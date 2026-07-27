using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Catalog.PaymentMethods.UpdatePaymentMethod;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Catalog.Specifications;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.UnitTests.Catalog;

public class UpdatePaymentMethodCommandHandlerTests
{
    private readonly IRepository<PaymentMethod, Guid> _paymentMethodRepository = Substitute.For<IRepository<PaymentMethod, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly UpdatePaymentMethodCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public UpdatePaymentMethodCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new UpdatePaymentMethodCommandHandler(_paymentMethodRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_MarkingAsDefault_ShouldUnmarkOtherDefaultsButNotItself()
    {
        var target = PaymentMethod.Create(_workspaceId, PaymentMethodType.CreditCard, "Visa").Value;
        var otherDefault = PaymentMethod.Create(_workspaceId, PaymentMethodType.Cash, "Cash", isDefault: true).Value;

        _paymentMethodRepository.GetByIdAsync(target.Id, Arg.Any<CancellationToken>()).Returns(target);
        _paymentMethodRepository.ListAsync(Arg.Any<DefaultPaymentMethodByWorkspaceSpecification>(), Arg.Any<CancellationToken>())
            .Returns([otherDefault]);

        var result = await _handler.Handle(new UpdatePaymentMethodCommand(target.Id, "Visa", true), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        target.IsDefault.Should().BeTrue();
        otherDefault.IsDefault.Should().BeFalse();
        _paymentMethodRepository.Received(1).Update(otherDefault);
        _paymentMethodRepository.Received(1).Update(target);
    }

    [Fact]
    public async Task Handle_WhenPaymentMethodNotFound_ShouldFailWithNotFound()
    {
        _paymentMethodRepository.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((PaymentMethod?)null);

        var result = await _handler.Handle(
            new UpdatePaymentMethodCommand(Guid.NewGuid(), "Visa", false), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("UpdatePaymentMethod.NotFound");
    }
}
