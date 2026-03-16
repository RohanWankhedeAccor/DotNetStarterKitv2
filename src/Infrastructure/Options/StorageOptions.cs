using System.ComponentModel.DataAnnotations;

namespace Infrastructure.Options;

/// <summary>
/// Strongly-typed configuration for the local file storage service.
/// Bound from the <c>Storage</c> section in appsettings.json.
///
/// <para>
/// In development, files are written to <see cref="BasePath"/> on the local disk.
/// In production, swap to a cloud implementation (Azure Blob Storage, AWS S3) by
/// changing the DI registration in <c>InfrastructureServiceExtensions</c> —
/// no handler code needs to change.
/// </para>
/// </summary>
public sealed class StorageOptions
{
    /// <summary>
    /// Root directory for locally stored files.
    /// Defaults to <c>uploads</c> (relative to the application working directory).
    /// Will be created on first use if it does not exist.
    /// </summary>
    [MinLength(1, ErrorMessage = "Storage:BasePath must not be empty.")]
    public string BasePath { get; init; } = "uploads";

    /// <summary>
    /// Maximum allowed upload size in bytes.
    /// Defaults to 10 MB (10 × 1024 × 1024).
    /// </summary>
    [Range(1, 1_073_741_824 /* 1 GiB */, ErrorMessage = "Storage:MaxFileSizeBytes must be between 1 byte and 1 GiB.")]
    public long MaxFileSizeBytes { get; init; } = 10 * 1024 * 1024;
}
