using FluentValidation;

namespace SubscriptionTracker.Application.Tenancy.InviteMember;

public sealed class InviteMemberCommandValidator : AbstractValidator<InviteMemberCommand>
{
    public InviteMemberCommandValidator()
    {
        RuleFor(c => c.Email).NotEmpty().EmailAddress();
        RuleFor(c => c.RoleId).NotEmpty();
    }
}
