using Application.Interfaces;
using Domain.Exceptions;
using Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Services;

/// <summary>
/// Local-disk implementation of <see cref="IFileStorageService"/>.
///
/// <para>
/// Files are stored under <see cref="StorageOptions.BasePath"/> using a
/// deterministic, collision-resistant key:
/// <c>{year}/{month}/{guid}{extension}</c> — e.g. <c>2026/03/a1b2c3.png</c>.
/// This path-based key is also used as the <see cref="StorageUploadResult.StorageKey"/>,
/// making it both a logical identifier and a relative path within the base directory.
/// </para>
///
/// <para>
/// A companion metadata file (<c>{key}.meta</c>) records the original file name
/// and content type so they can be returned verbatim on download.
/// </para>
///
/// <para>
/// Intended for development and small-scale production deployments.
/// To use Azure Blob Storage or S3 in production, implement <see cref="IFileStorageService"/>
/// and swap the DI registration in <c>InfrastructureServiceExtensions</c>.
/// </para>
/// </summary>
public sealed class LocalFileStorageService : IFileStorageService
{
    private readonly StorageOptions _options;
    private readonly ILogger<LocalFileStorageService> _logger;

    /// <summary>
    /// Separator used between fields in the companion metadata file.
    /// Chosen to avoid collisions with file names or MIME types.
    /// </summary>
    private const string MetaSeparator = "\n";

    /// <summary>
    /// Initializes the service with resolved storage configuration.
    /// </summary>
    public LocalFileStorageService(IOptions<StorageOptions> options, ILogger<LocalFileStorageService> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StorageUploadResult> UploadAsync(
        Stream stream,
        string fileName,
        string? contentType = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);

        var resolvedContentType = contentType ?? InferContentType(fileName);
        var extension = Path.GetExtension(fileName);
        var now = DateTimeOffset.UtcNow;

        // Build a time-bucketed, collision-resistant storage key.
        var storageKey = $"{now:yyyy}/{now:MM}/{Guid.NewGuid()}{extension}";
        var fullPath = ToFullPath(storageKey);

        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await stream.CopyToAsync(fileStream, cancellationToken);
        var sizeBytes = fileStream.Position;

        // Persist metadata alongside the file so downloads can reconstruct it.
        var metaPath = fullPath + ".meta";
        await File.WriteAllTextAsync(metaPath, $"{fileName}{MetaSeparator}{resolvedContentType}", cancellationToken);

        _logger.LogInformation(
            "File uploaded: key={StorageKey}, name={FileName}, type={ContentType}, size={Bytes} bytes",
            storageKey, fileName, resolvedContentType, sizeBytes);

        return new StorageUploadResult(storageKey, resolvedContentType, sizeBytes);
    }

    /// <inheritdoc />
    public async Task<StorageDownloadResult> DownloadAsync(
        string storageKey,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var fullPath = ToFullPath(storageKey);
        if (!File.Exists(fullPath))
            throw new NotFoundException("File", storageKey);

        var (originalFileName, contentType) = await ReadMetaAsync(fullPath, cancellationToken);

        // FileStream with DeleteOnClose is NOT used here — the caller might stream
        // the response across multiple async continuations. The stream is left open
        // and it's the caller's responsibility to dispose it.
        var fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read);

        return new StorageDownloadResult(fileStream, contentType, originalFileName);
    }

    /// <inheritdoc />
    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(storageKey);

        var fullPath = ToFullPath(storageKey);

        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);

            var metaPath = fullPath + ".meta";
            if (File.Exists(metaPath))
                File.Delete(metaPath);

            _logger.LogInformation("File deleted: key={StorageKey}", storageKey);
        }

        return Task.CompletedTask;
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private string ToFullPath(string storageKey)
        => Path.GetFullPath(Path.Combine(_options.BasePath, storageKey));

    private static async Task<(string fileName, string contentType)> ReadMetaAsync(
        string filePath,
        CancellationToken cancellationToken)
    {
        var metaPath = filePath + ".meta";
        if (!File.Exists(metaPath))
            return (Path.GetFileName(filePath), "application/octet-stream");

        var raw = await File.ReadAllTextAsync(metaPath, cancellationToken);
        var parts = raw.Split(MetaSeparator, 2);
        return parts.Length == 2
            ? (parts[0], parts[1])
            : (Path.GetFileName(filePath), "application/octet-stream");
    }

    /// <summary>
    /// Returns a best-effort MIME type derived from the file extension.
    /// Falls back to <c>application/octet-stream</c> for unknown extensions.
    /// </summary>
    private static string InferContentType(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".webp"           => "image/webp",
            ".svg"            => "image/svg+xml",
            ".pdf"            => "application/pdf",
            ".txt"            => "text/plain",
            ".html"           => "text/html",
            ".json"           => "application/json",
            ".csv"            => "text/csv",
            ".zip"            => "application/zip",
            ".docx"           => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            ".xlsx"           => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _                 => "application/octet-stream",
        };
    }
}
