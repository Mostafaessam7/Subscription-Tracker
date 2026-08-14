namespace SubscriptionTracker.Infrastructure.Storage;

/// <summary>
/// The file-extension allow-list shared by every IFileStorageService implementation (LocalFileStorageService,
/// AzureBlobFileStorageService) so the "on-disk/blob name is always a fresh Guid + a sanitized extension, never
/// the caller-supplied filename" convention can't drift between providers - previously duplicated verbatim in
/// each class.
/// </summary>
internal static class StoredFileNameSanitizer
{
    public static string SanitizeExtension(string extension)
    {
        if (string.IsNullOrEmpty(extension) || extension.Length > 10)
        {
            return string.Empty;
        }

        return extension.All(c => char.IsLetterOrDigit(c) || c == '.') ? extension : string.Empty;
    }
}
