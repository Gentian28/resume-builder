using FluentAssertions;
using ResumeBuilder.Core.Sync;

namespace ResumeBuilder.Tests;

public class LocalFolderSyncServiceTests : IDisposable
{
    private readonly string _testFolder;
    private readonly LocalFolderSyncService _service;

    public LocalFolderSyncServiceTests()
    {
        _testFolder = Path.Combine(Path.GetTempPath(), $"ResumeBuilderTest_{Guid.NewGuid()}");
        _service = new LocalFolderSyncService();
    }

    public void Dispose()
    {
        if (Directory.Exists(_testFolder))
        {
            Directory.Delete(_testFolder, true);
        }
    }

    [Fact]
    public void ProviderName_ReturnsLocalFolder()
    {
        _service.ProviderName.Should().Be("Local Folder");
    }

    [Fact]
    public void IsConfigured_BeforeConfiguration_ReturnsFalse()
    {
        _service.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task ConfigureAsync_ValidPath_ReturnsTrue()
    {
        // Act
        var result = await _service.ConfigureAsync(_testFolder);

        // Assert
        result.Should().BeTrue();
        _service.IsConfigured.Should().BeTrue();
        Directory.Exists(_testFolder).Should().BeTrue();
    }

    [Fact]
    public async Task ConfigureAsync_CreatesDirectory_IfNotExists()
    {
        // Arrange
        var newFolder = Path.Combine(_testFolder, "subfolder");

        // Act
        await _service.ConfigureAsync(newFolder);

        // Assert
        Directory.Exists(newFolder).Should().BeTrue();
    }

    [Fact]
    public async Task UploadAsync_NotConfigured_ReturnsFalse()
    {
        // Act
        var result = await _service.UploadAsync("content", "test.json");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task UploadAsync_Configured_CreatesFile()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);
        var content = "{ \"test\": true }";
        var fileName = "test.json";

        // Act
        var result = await _service.UploadAsync(content, fileName);

        // Assert
        result.Should().BeTrue();
        var filePath = Path.Combine(_testFolder, fileName);
        File.Exists(filePath).Should().BeTrue();
        File.ReadAllText(filePath).Should().Be(content);
    }

    [Fact]
    public async Task DownloadAsync_NotConfigured_ReturnsNull()
    {
        // Act
        var result = await _service.DownloadAsync("test.json");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DownloadAsync_ExistingFile_ReturnsContent()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);
        var content = "{ \"downloaded\": true }";
        File.WriteAllText(Path.Combine(_testFolder, "test.json"), content);

        // Act
        var result = await _service.DownloadAsync("test.json");

        // Assert
        result.Should().Be(content);
    }

    [Fact]
    public async Task DownloadAsync_NonExistingFile_ReturnsNull()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);

        // Act
        var result = await _service.DownloadAsync("nonexistent.json");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ListRemoteAsync_NotConfigured_ReturnsEmpty()
    {
        // Act
        var result = await _service.ListRemoteAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRemoteAsync_EmptyFolder_ReturnsEmpty()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);

        // Act
        var result = await _service.ListRemoteAsync();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ListRemoteAsync_WithFiles_ReturnsFileInfo()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);
        File.WriteAllText(Path.Combine(_testFolder, "resume1.json"), "{}");
        File.WriteAllText(Path.Combine(_testFolder, "resume2.json"), "{ \"name\": \"test\" }");

        // Act
        var result = (await _service.ListRemoteAsync()).ToList();

        // Assert
        result.Should().HaveCount(2);
        result.Should().Contain(r => r.Name == "resume1");
        result.Should().Contain(r => r.Name == "resume2");
    }

    [Fact]
    public async Task ListRemoteAsync_OnlyListsJsonFiles()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);
        File.WriteAllText(Path.Combine(_testFolder, "resume.json"), "{}");
        File.WriteAllText(Path.Combine(_testFolder, "other.txt"), "text");
        File.WriteAllText(Path.Combine(_testFolder, "image.png"), "binary");

        // Act
        var result = (await _service.ListRemoteAsync()).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Name.Should().Be("resume");
    }

    [Fact]
    public async Task DeleteRemoteAsync_NotConfigured_ReturnsFalse()
    {
        // Act
        var result = await _service.DeleteRemoteAsync("test.json");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRemoteAsync_ExistingFile_DeletesAndReturnsTrue()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);
        var filePath = Path.Combine(_testFolder, "todelete.json");
        File.WriteAllText(filePath, "{}");

        // Act
        var result = await _service.DeleteRemoteAsync("todelete.json");

        // Assert
        result.Should().BeTrue();
        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteRemoteAsync_NonExistingFile_ReturnsFalse()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);

        // Act
        var result = await _service.DeleteRemoteAsync("nonexistent.json");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task Status_BeforeConfiguration_IsNotConfigured()
    {
        // Assert
        _service.Status.Should().Be(SyncStatus.NotConfigured);
    }

    [Fact]
    public async Task Status_AfterConfiguration_IsIdle()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);

        // Assert
        _service.Status.Should().Be(SyncStatus.Idle);
    }

    [Fact]
    public async Task StatusChanged_EventFired_OnConfiguration()
    {
        // Arrange
        SyncStatus? receivedStatus = null;
        _service.StatusChanged += s => receivedStatus = s;

        // Act
        await _service.ConfigureAsync(_testFolder);

        // Assert
        receivedStatus.Should().Be(SyncStatus.Idle);
    }

    [Fact]
    public async Task UploadAsync_SanitizesFileName()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);
        var unsafeName = "resume:with<invalid>chars.json";

        // Act
        var result = await _service.UploadAsync("{}", unsafeName);

        // Assert
        result.Should().BeTrue();
        // The file should exist with sanitized name
        Directory.GetFiles(_testFolder, "*.json").Should().HaveCount(1);
    }

    [Fact]
    public async Task RoundTrip_UploadThenDownload_PreservesContent()
    {
        // Arrange
        await _service.ConfigureAsync(_testFolder);
        var originalContent = "{ \"roundtrip\": \"test\", \"number\": 42 }";

        // Act
        await _service.UploadAsync(originalContent, "roundtrip.json");
        var downloaded = await _service.DownloadAsync("roundtrip.json");

        // Assert
        downloaded.Should().Be(originalContent);
    }
}

public class SyncResultTests
{
    [Fact]
    public void Succeeded_CreatesSucessResult()
    {
        // Act
        var result = SyncResult.Succeeded(uploaded: 5, downloaded: 3);

        // Assert
        result.Success.Should().BeTrue();
        result.Status.Should().Be(SyncStatus.Success);
        result.UploadedCount.Should().Be(5);
        result.DownloadedCount.Should().Be(3);
    }

    [Fact]
    public void Failed_CreatesFailedResult()
    {
        // Act
        var result = SyncResult.Failed("Test error message");

        // Assert
        result.Success.Should().BeFalse();
        result.Status.Should().Be(SyncStatus.Error);
        result.Message.Should().Be("Test error message");
    }

    [Fact]
    public void NotConfigured_CreatesNotConfiguredResult()
    {
        // Act
        var result = SyncResult.NotConfigured();

        // Assert
        result.Success.Should().BeFalse();
        result.Status.Should().Be(SyncStatus.NotConfigured);
    }
}
