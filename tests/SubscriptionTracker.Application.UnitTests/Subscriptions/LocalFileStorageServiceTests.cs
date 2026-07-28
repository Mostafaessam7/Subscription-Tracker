using FluentAssertions;
using Microsoft.Extensions.Options;
using SubscriptionTracker.Infrastructure.Storage;

namespace SubscriptionTracker.Application.UnitTests.Subscriptions;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _tempRoot = Path.Combine(Path.GetTempPath(), $"st-attachments-tests-{Guid.NewGuid():N}");
    private readonly LocalFileStorageService _service;

    public LocalFileStorageServiceTests()
    {
        _service = new LocalFileStorageService(Options.Create(new FileStorageOptions { RootPath = _tempRoot }));
    }

    [Fact]
    public async Task SaveAsync_ThenReadAsync_ShouldRoundTripContent()
    {
        byte[] content = [1, 2, 3, 4, 5];

        var storagePath = await _service.SaveAsync(content, "receipt.pdf", CancellationToken.None);
        var readBack = await _service.ReadAsync(storagePath, CancellationToken.None);

        readBack.Should().BeEquivalentTo(content);
        storagePath.Should().EndWith(".pdf");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTheFile()
    {
        var storagePath = await _service.SaveAsync([1, 2, 3], "receipt.pdf", CancellationToken.None);

        await _service.DeleteAsync(storagePath, CancellationToken.None);

        await FluentActions.Awaiting(() => _service.ReadAsync(storagePath, CancellationToken.None))
            .Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task SaveAsync_WithPathTraversalAttemptInFileName_ShouldNotEscapeRootDirectory()
    {
        var storagePath = await _service.SaveAsync([1, 2, 3], "../../evil.exe", CancellationToken.None);

        storagePath.Should().NotContain("..");
        Directory.GetFiles(_tempRoot).Should().ContainSingle();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempRoot))
        {
            Directory.Delete(_tempRoot, recursive: true);
        }

        GC.SuppressFinalize(this);
    }
}
