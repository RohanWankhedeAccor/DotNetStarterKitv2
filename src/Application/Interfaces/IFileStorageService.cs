namespace Application.Interfaces;

/// <summary>
/// Result returned by a successful file upload.
/// </summary>
/// <param name="StorageKey">
/// A unique, provider-opaque identifier for the stored file.
/// Pass this back to <see cref="IFileStorageService.DownloadAsync"/> or
/// <see cref="IFileStorageService.DeleteAsync"/>.
/// </param>
/// <param name="ContentType">MIME type of the stored file (e.g. <c>image/png</c>).</param>
/// <param name="SizeBytes">Size of the stored file in bytes.</param>
public sealed record StorageUploadResult(
    string StorageKey,
    string ContentType,
    long SizeBytes);

/// <summary>
/// Result returned when downloading a file.
/// </summary>
/// <param name="Stream">
/// A readable stream containing the file bytes.
/// The caller is responsible for disposing the stream.
/// </param>
/// <param name="ContentType">MIME type of the file.</param>
/// <param name="FileName">Original file name (for use in <c>Content-Disposition</c> headers).</param>
public sealed record StorageDownloadResult(
    Stream Stream,
    string ContentType,
    string FileName);

/// <summary>
/// Abstraction for storing and retrieving binary files.
///
/// <para>
/// Handlers depend on this interface rather than any cloud SDK, keeping the
/// Application layer free of Infrastructure concerns. The Infrastructure layer
/// provides a local-disk implementation (<c>LocalFileStorageService</c>) for
/// development and test, and can be swapped for Azure Blob Storage, S3, or
/// any other provider without changing handler code.
/// </para>
///
/// <para><b>Key conventions</b></para>
/// <list type="bullet">
///   <item>
///     The <see cref="StorageUploadResult.StorageKey"/> is the canonical identifier.
///     Do not assume a file path or URL structure — treat it as opaque.
///   </item>
///   <item>
///     Streams returned by <see cref="DownloadAsync"/> must be disposed by the caller.
///   </item>
/// </list>
/// </summary>
public interface IFileStorageService
{
    /// <summary>
    /// Stores <paramref name="stream"/> under the given <paramref name="fileName"/>
    /// and returns a <see cref="StorageUploadResult"/> containing the assigned storage key.
    /// </summary>
    /// <param name="stream">The file content to store. Must be readable.</param>
    /// <param name="fileName">
    /// The original file name including extension (e.g. <c>avatar.png</c>).
    /// Used to derive the content type and preserve the name for later downloads.
    /// </param>
    /// <param name="contentType">
    /// MIME type of the content (e.g. <c>image/png</c>).
    /// If <c>null</c>, the implementation may infer it from <paramref name="fileName"/>.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A <see cref="StorageUploadResult"/> with the assigned storage key.</returns>
    Task<StorageUploadResult> UploadAsync(
        Stream stream,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the file identified by <paramref name="storageKey"/>.
    /// </summary>
    /// <param name="storageKey">
    /// The key returned by a previous <see cref="UploadAsync"/> call.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>
    /// A <see cref="StorageDownloadResult"/> whose <c>Stream</c> must be disposed by the caller.
    /// </returns>
    /// <exception cref="Domain.Exceptions.NotFoundException">
    /// Thrown when no file with <paramref name="storageKey"/> exists.
    /// </exception>
    Task<StorageDownloadResult> DownloadAsync(
        string storageKey,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Permanently deletes the file identified by <paramref name="storageKey"/>.
    /// No-op when the key does not exist.
    /// </summary>
    /// <param name="storageKey">The key returned by a previous <see cref="UploadAsync"/> call.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(
        string storageKey,
        CancellationToken cancellationToken = default);
}
