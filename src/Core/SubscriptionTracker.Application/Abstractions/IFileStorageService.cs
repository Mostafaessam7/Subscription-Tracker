namespace SubscriptionTracker.Application.Abstractions;

/// <summary>Local/blob-agnostic file storage for subscription attachments. Only one implementation exists today
/// (local disk, see SubscriptionTracker.Infrastructure.Storage.LocalFileStorageService) but the abstraction keeps
/// the door open for swapping in blob storage later without touching Application-layer command handlers.</summary>
public interface IFileStorageService
{
    /// <summary>Saves the content under a storage-generated path (never the caller-supplied file name, to avoid
    /// path traversal and collisions) and returns that path for later retrieval/deletion.</summary>
    Task<string> SaveAsync(byte[] content, string originalFileName, CancellationToken cancellationToken = default);

    Task<byte[]> ReadAsync(string storagePath, CancellationToken cancellationToken = default);

    Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default);
}
