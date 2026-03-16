using Domain.Exceptions;
using FluentAssertions;
using Infrastructure.Options;
using Infrastructure.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace Unit.Storage;

/// <summary>
/// Unit tests for <see cref="LocalFileStorageService"/>.
/// Each test uses an isolated temp directory so tests do not interfere with
/// each other and the working tree is kept clean.
/// </summary>
public sealed class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), $"storage-tests-{Guid.NewGuid()}");
    private readonly LocalFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        var options = Options.Create(new StorageOptions { BasePath = _tempDir });
        _sut = new LocalFileStorageService(options, NullLogger<LocalFileStorageService>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Upload ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task UploadAsync_WritesFileToDisk()
    {
        using var content = new MemoryStream("hello"u8.ToArray());

        var result = await _sut.UploadAsync(content, "hello.txt");

        var fullPath = Path.Combine(_tempDir, result.StorageKey);
        File.Exists(fullPath).Should().BeTrue("file should be persisted to disk");
    }

    [Fact]
    public async Task UploadAsync_ReturnsCorrectSizeBytes()
    {
        var bytes = "hello world"u8.ToArray();
        using var content = new MemoryStream(bytes);

        var result = await _sut.UploadAsync(content, "test.txt");

        result.SizeBytes.Should().Be(bytes.Length);
    }

    [Fact]
    public async Task UploadAsync_InfersContentTypeFromExtension()
    {
        using var content = new MemoryStream([0x89, 0x50, 0x4E, 0x47]); // PNG header bytes

        var result = await _sut.UploadAsync(content, "photo.png");

        result.ContentType.Should().Be("image/png");
    }

    [Fact]
    public async Task UploadAsync_UsesProvidedContentTypeOverExtension()
    {
        using var content = new MemoryStream([1, 2, 3]);

        var result = await _sut.UploadAsync(content, "data.bin", "application/octet-stream");

        result.ContentType.Should().Be("application/octet-stream");
    }

    [Fact]
    public async Task UploadAsync_ReturnsDifferentKeysForEachUpload()
    {
        using var content1 = new MemoryStream("a"u8.ToArray());
        using var content2 = new MemoryStream("b"u8.ToArray());

        var r1 = await _sut.UploadAsync(content1, "a.txt");
        var r2 = await _sut.UploadAsync(content2, "b.txt");

        r1.StorageKey.Should().NotBe(r2.StorageKey, "each upload generates a unique GUID");
    }

    [Fact]
    public async Task UploadAsync_WithNullStream_ThrowsArgumentNullException()
    {
        var act = async () => await _sut.UploadAsync(null!, "file.txt");

        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task UploadAsync_WithBlankFileName_ThrowsArgumentException()
    {
        using var content = new MemoryStream("x"u8.ToArray());

        var act = async () => await _sut.UploadAsync(content, "  ");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    // ── Download ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task DownloadAsync_ReturnsOriginalContent()
    {
        var originalBytes = "round-trip content"u8.ToArray();
        using var upload = new MemoryStream(originalBytes);
        var uploadResult = await _sut.UploadAsync(upload, "data.txt");

        var download = await _sut.DownloadAsync(uploadResult.StorageKey);
        await using (download.Stream)
        {
            var downloaded = new MemoryStream();
            await download.Stream.CopyToAsync(downloaded);
            downloaded.ToArray().Should().Equal(originalBytes);
        }
    }

    [Fact]
    public async Task DownloadAsync_ReturnsOriginalFileName()
    {
        using var content = new MemoryStream("x"u8.ToArray());
        var uploadResult = await _sut.UploadAsync(content, "original-name.pdf");

        var download = await _sut.DownloadAsync(uploadResult.StorageKey);
        await using (download.Stream) { }

        download.FileName.Should().Be("original-name.pdf");
    }

    [Fact]
    public async Task DownloadAsync_ReturnsCorrectContentType()
    {
        using var content = new MemoryStream("x"u8.ToArray());
        var uploadResult = await _sut.UploadAsync(content, "report.pdf");

        var download = await _sut.DownloadAsync(uploadResult.StorageKey);
        await using (download.Stream) { }

        download.ContentType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task DownloadAsync_WithNonExistentKey_ThrowsNotFoundException()
    {
        var act = async () => await _sut.DownloadAsync("2099/01/does-not-exist.txt");

        await act.Should().ThrowAsync<NotFoundException>();
    }

    // ── Delete ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_RemovesFileFromDisk()
    {
        using var content = new MemoryStream("bye"u8.ToArray());
        var uploadResult = await _sut.UploadAsync(content, "bye.txt");
        var fullPath = Path.Combine(_tempDir, uploadResult.StorageKey);

        await _sut.DeleteAsync(uploadResult.StorageKey);

        File.Exists(fullPath).Should().BeFalse("file should be removed after deletion");
    }

    [Fact]
    public async Task DeleteAsync_AfterDelete_DownloadThrowsNotFoundException()
    {
        using var content = new MemoryStream("x"u8.ToArray());
        var uploadResult = await _sut.UploadAsync(content, "file.txt");
        await _sut.DeleteAsync(uploadResult.StorageKey);

        var act = async () => await _sut.DownloadAsync(uploadResult.StorageKey);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentKey_DoesNotThrow()
    {
        var act = async () => await _sut.DeleteAsync("2099/01/never-existed.txt");

        await act.Should().NotThrowAsync("delete is a no-op for missing files");
    }
}
