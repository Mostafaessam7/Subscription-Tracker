using FluentValidation;

namespace SubscriptionTracker.Application.Subscriptions.UploadAttachment;

public sealed class UploadAttachmentCommandValidator : AbstractValidator<UploadAttachmentCommand>
{
    private const long MaxSizeBytes = 10 * 1024 * 1024; // 10 MB

    public UploadAttachmentCommandValidator()
    {
        RuleFor(c => c.FileName).NotEmpty().MaximumLength(255);
        RuleFor(c => c.ContentType).NotEmpty().MaximumLength(127);
        RuleFor(c => c.Content).NotEmpty();
        RuleFor(c => (long)c.Content.Length).LessThanOrEqualTo(MaxSizeBytes)
            .WithMessage("Attachments cannot exceed 10 MB.");
    }
}
