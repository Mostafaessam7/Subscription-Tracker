using FluentAssertions;
using NSubstitute;
using SubscriptionTracker.Application.Abstractions;
using SubscriptionTracker.Application.Catalog.PaymentMethods.CreatePaymentMethod;
using SubscriptionTracker.Domain.Catalog;
using SubscriptionTracker.Domain.Catalog.Specifications;
using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Application.UnitTests.Catalog;

public class CreatePaymentMethodCommandHandlerTests
{
    private readonly IRepository<PaymentMethod, Guid> _paymentMethodRepository = Substitute.For<IRepository<PaymentMethod, Guid>>();
    private readonly ICurrentUserService _currentUserService = Substitute.For<ICurrentUserService>();

    private readonly CreatePaymentMethodCommandHandler _handler;
    private readonly Guid _workspaceId = Guid.NewGuid();

    public CreatePaymentMethodCommandHandlerTests()
    {
        _currentUserService.WorkspaceId.Returns(_workspaceId);
        _handler = new CreatePaymentMethodCommandHandler(_paymentMethodRepository, _currentUserService);
    }

    [Fact]
    public async Task Handle_WithoutDefaultFlag_ShouldNotQueryExistingDefaults()
    {
        var command = new CreatePaymentMethodCommand(PaymentMethodType.CreditCard, "Visa", "•••• 4242", false);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _paymentMethodRepository.DidNotReceive().ListAsync(
            Arg.Any<Specification<PaymentMethod>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDefaultFlag_ShouldUnmarkExistingDefaults()
    {
        var existingDefault = PaymentMethod.Create(_workspaceId, PaymentMethodType.Cash, "Cash", isDefault: true).Value;
        _paymentMethodRepository.ListAsync(Arg.Any<DefaultPaymentMethodByWorkspaceSpecification>(), Arg.Any<CancellationToken>())
            .Returns([existingDefault]);

        var command = new CreatePaymentMethodCommand(PaymentMethodType.CreditCard, "Visa", "•••• 4242", true);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        existingDefault.IsDefault.Should().BeFalse();
        _paymentMethodRepository.Received(1).Update(existingDefault);
        _paymentMethodRepository.Received(1).Add(Arg.Is<PaymentMethod>(p => p.IsDefault));
    }
}
