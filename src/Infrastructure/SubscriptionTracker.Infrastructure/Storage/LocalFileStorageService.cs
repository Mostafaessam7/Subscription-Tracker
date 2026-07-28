using Microsoft.Extensions.Options;
using SubscriptionTracker.Application.Abstractions;

namespace SubscriptionTracker.Infrastructure.Storage;

/// <summary>Writes attachments to a local directory. The on-disk filename is always a fresh Guid - the caller's
/// original filename is stored separately (SubscriptionAttachment.FileName) and never trusted as a path
/// component, which is what keeps this safe from path-traversal regardless of what a client sends.</summary>
public sealed class LocalFileStorageService(IOptions<FileStorageOptions> options) : IFileStorageService
{
    private readonly string _rootPath = Path.GetFullPath(options.Value.RootPath);

    public async Task<string> SaveAsync(byte[] content, string originalFileName, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        var extension = SanitizeExtension(Path.GetExtension(originalFileName));
        var storagePath = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_rootPath, storagePath);

        await File.WriteAllBytesAsync(fullPath, content, cancellationToken);

        return storagePath;
    }

    public async Task<byte[]> ReadAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(storagePath);
        return await File.ReadAllBytesAsync(fullPath, cancellationToken);
    }

    public Task DeleteAsync(string storagePath, CancellationToken cancellationToken = default)
    {
        var fullPath = ResolveFullPath(storagePath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    private string ResolveFullPath(string storagePath)
    {
        // storagePath is always a bare Guid-based filename we generated in SaveAsync (see the class comment) -
        // GetFileName() strips any directory components a corrupted/tampered value might otherwise smuggle in,
        // so the result can never resolve outside _rootPath.
        var safeFileName = Path.GetFileName(storagePath);
        return Path.Combine(_rootPath, safeFileName);
    }

    private static string SanitizeExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension) || extension.Length > 10)
        {
            return string.Empty;
        }

        return extension.All(c => char.IsLetterOrDigit(c) || c == '.') ? extension : string.Empty;
    }
}
