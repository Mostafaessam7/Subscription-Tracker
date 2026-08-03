using FluentValidation;

namespace SubscriptionTracker.Application.Tenancy.Roles.UpdateRole;

public sealed class UpdateRoleCommandValidator : AbstractValidator<UpdateRoleCommand>
{
    public UpdateRoleCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty().MaximumLength(100);
        RuleFor(c => c.Description).MaximumLength(500);
        RuleFor(c => c.PermissionCodes).NotNull();
        RuleForEach(c => c.PermissionCodes)
            .Must(code => Domain.Identity.Permissions.All.Contains(code))
            .WithMessage("'{PropertyValue}' is not a recognized permission code.");
    }
}
