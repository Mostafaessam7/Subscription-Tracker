using SubscriptionTracker.Domain.Common;

namespace SubscriptionTracker.Domain.Subscriptions;

public sealed class SubscriptionAttachment : Entity<Guid>
{
    private SubscriptionAttachment(
        Guid id, Guid subscriptionId, string fileName, string contentType, long sizeBytes, string storagePath, Guid uploadedBy)
        : base(id)
    {
        SubscriptionId = subscriptionId;
        FileName = fileName;
        ContentType = contentType;
        SizeBytes = sizeBytes;
        StoragePath = storagePath;
        UploadedBy = uploadedBy;
        UploadedAtUtc = DateTimeOffset.UtcNow;
    }

    private SubscriptionAttachment()
    {
    }

    public Guid SubscriptionId { get; private set; }
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string StoragePath { get; private set; } = string.Empty;
    public Guid UploadedBy { get; private set; }
    public DateTimeOffset UploadedAtUtc { get; private set; }

    internal static Result<SubscriptionAttachment> Create(
        Guid subscriptionId, string fileName, string contentType, long sizeBytes, string storagePath, Guid uploadedBy)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return Result.Failure<SubscriptionAttachment>(
                Error.Validation("SubscriptionAttachment.EmptyFileName", "File name cannot be empty."));
        }

        if (sizeBytes <= 0)
        {
            return Result.Failure<SubscriptionAttachment>(
                Error.Validation("SubscriptionAttachment.InvalidSize", "File size must be greater than zero."));
        }

        return new SubscriptionAttachment(Guid.NewGuid(), subscriptionId, fileName.Trim(), contentType, sizeBytes, storagePath, uploadedBy);
    }
}
