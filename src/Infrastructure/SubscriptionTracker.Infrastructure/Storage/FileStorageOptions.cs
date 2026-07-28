namespace SubscriptionTracker.Infrastructure.Storage;

public sealed class FileStorageOptions
{
    public const string SectionName = "FileStorage";

    /// <summary>Root directory attachments are written under. Relative paths are resolved against the current
    /// working directory at startup. Must be writable by the process (see HANDOVER.md for the Docker non-root
    /// user caveat that also applies to this path, same as the Serilog file sink).</summary>
    public string RootPath { get; set; } = "storage/attachments";
}
