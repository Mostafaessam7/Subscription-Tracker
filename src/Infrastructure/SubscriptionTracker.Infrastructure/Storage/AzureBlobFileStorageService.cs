using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using SubscriptionTracker.Application.Abstractions;

namespace SubscriptionTracker.Infrastructure.Storage;

/// <summary>
/// Durable, replica-safe attachment storage backed by Azure Blob Storage - the alternative to
/// LocalFileStorageService for deployments that run more than one API instance or need attachments to survive
/// a container redeploy (see the FileStorage:RootPath gap documented in HANDOVER.md). Selected via
/// FileStorage:Provider = AzureBlob.
///
/// Same on-disk-filename safety property as LocalFileStorageService: the blob name is always a fresh Guid, the
/// caller's original filename is never used as a blob name, so a tampered/corrupted storagePath value can at
/// worst 404 - there's no path-traversal-equivalent risk with blob names the way there is with filesystem
/// paths, but keeping the same "always a fresh Guid" convention avoids the two implementations drifting.
/// </summary>
public sealed class AzureBlobFileStorageService : IFileStorageService
{
    private readonly BlobContainerClient _containerClient;

    // Ensures the container exists exactly once per process instead of on every SaveAsync call - this service
    // is registered as a singleton (see DependencyInjection.AddFileStorage), so one Lazy<Task> here covers the
    // whole app's lifetime. Lazy<Task> (not a bool flag) so concurrent first-callers all await the same
    // in-flight creation instead of racing separate CreateIfNotExistsAsync calls.
    private readonly Lazy<Task> _containerExists;

    public AzureBlobFileStorageService(IOptions<FileStorageOptions> options)
    {
        var blobOptions = options.Value.Blob;
        _containerClient = new BlobContainerClient(blobOptions.ConnectionString, blobOptions.ContainerName);
        _containerExists = new Lazy<Task>(() => _containerClient.CreateIfNotExistsAsync());
    }

    public async Task<string> SaveAsync(byte[] content, string originalFileName, CancellationToken cancellationToken = default)
    {
        await _containerExists.Value;

        var extension = StoredFileNameSanitizer.SanitizeExtension(Path.GetExtension(originalFileName));
        var blobName = $"{Guid.NewGuid():N}{extension}";

        var blobClient = _containerClient.GetBlobClient(blobName);
        using var stream = new MemoryStream(content);
        await blobClient.UploadAsync(stream, overwrite: false, cancellationToken);

        return blobName;
    }

    public async Task<byte[]> ReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(SanitizeBlobName(storagePath));
        var response = await blobClient.DownloadContentAsync(cancellationToken);
        return response.Value.Content.ToArray();
    }

    public async Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var blobClient = _containerClient.GetBlobClient(SanitizeBlobName(storagePath));
        await blobClient.DeleteIfExistsAsync(cancellationToken: cancellationToken);
    }

    // storagePath is always a bare Guid-based blob name we generated in SaveAsync (see the class comment) -
    // strip any path separators a corrupted/tampered value might otherwise carry, mirroring
    // LocalFileStorageService.ResolveFullPath's use of Path.GetFileName() for the same reason.
    private static string SanitizeBlobName(string storagePath) => Path.GetFileName(storagePath);
}
