namespace SubscriptionTracker.Infrastructure.Storage;

public enum FileStorageProvider
{
    /// <summary>Local disk (default). Fine for a single instance; does not survive a container
    /// redeploy/restart unless RootPath is a mounted persistent volume, and does not work across multiple
    /// API replicas (each has its own disk) - see HANDOVER.md.</summary>
    Local,

    /// <summary>Azure Blob Storage - durable and replica-safe. Requires FileStorage:Blob:ConnectionString
    /// (and optionally FileStorage:Blob:ContainerName) to be configured.</summary>
    AzureBlob,
}

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    public FileStorageProvider Provider { get; set; } = FileStorageProvider.Local;

    /// <summary>Root directory attachments are written under when Provider is Local. Relative paths are
    /// resolved against the current working directory at startup. Must be writable by the process (see
    /// HANDOVER.md for the Docker non-root user caveat that also applies to this path, same as the Serilog
    /// file sink).</summary>
    public string RootPath { get; set; } = "storage/attachments";

    public BlobStorageOptions Blob { get; set; } = new();
}

public sealed class BlobStorageOptions
{
    /// <summary>Azure Storage connection string. Required when Provider is AzureBlob - the app fails fast at
    /// startup (see FileStorageProviderValidator) rather than discovering it's missing on first upload.</summary>
    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "subscription-attachments";
}

/// <summary>Extracted from DependencyInjection.AddFileStorage so the fail-fast check is unit-testable without
/// spinning up a full host - same rationale as SubscriptionTracker.Api.Startup.ProductionSecretsGuard.</summary>
public static class FileStorageProviderValidator
{
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="provider"/> is AzureBlob and <paramref name="blobConnectionString"/> is
    /// missing.
    /// </exception>
    public static void EnsureConfigured(FileStorageProvider provider, string? blobConnectionString)
    {
        if (provider == FileStorageProvider.AzureBlob && string.IsNullOrWhiteSpace(blobConnectionString))
        {
            throw new InvalidOperationException(
                "FileStorage:Provider is set to AzureBlob but FileStorage:Blob:ConnectionString is missing. " +
                "Set it via the FileStorage__Blob__ConnectionString environment variable (or another " +
                "configuration provider) before starting the app.");
        }
    }
}
