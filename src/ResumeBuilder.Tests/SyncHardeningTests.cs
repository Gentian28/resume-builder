using System.Text.Json;
using AwesomeAssertions;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.Sync;

namespace ResumeBuilder.Tests;

/// <summary>
/// The 2 September assessment found five ways a shared folder could not be trusted: half-written
/// files, ids minted afresh on every sync, cloud conflict copies silently winning, and deletions on
/// either side coming straight back. Each has a test here.
/// </summary>
public class SyncHardeningTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), $"SyncHardening_{Guid.NewGuid():N}");
    private readonly Store _store = new();
    private readonly LocalFolderSyncService _service;

    public SyncHardeningTests()
    {
        _service = new LocalFolderSyncService(_store, new SyncStateStore(Path.Combine(_folder, "state", "sync-state.json")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
            Directory.Delete(_folder, recursive: true);
    }

    [Fact]
    public async Task Upload_LeavesNoTemporaryFileBehind()
    {
        await _service.ConfigureAsync(_folder);
        _store.Seed("Whole");

        await _service.SyncAllAsync();

        Directory.GetFiles(_folder, "*" + AtomicFile.TemporarySuffix).Should().BeEmpty();
        Directory.GetFiles(_folder, "*.json").Should().ContainSingle();
    }

    [Fact]
    public async Task AtomicWrite_ReplacesTheFileWhole()
    {
        Directory.CreateDirectory(_folder);
        var path = Path.Combine(_folder, "a.json");
        await File.WriteAllTextAsync(path, "old");

        await AtomicFile.WriteAllTextAsync(path, "new content");

        (await File.ReadAllTextAsync(path)).Should().Be("new content");
        File.Exists(path + AtomicFile.TemporarySuffix).Should().BeFalse();
    }

    [Fact]
    public async Task RemoteFileWithoutASyncId_IsAdoptedOnce_NotOnEverySync()
    {
        await _service.ConfigureAsync(_folder);
        var exported = new Resume { Name = "Exported before sync ids", UpdatedAt = DateTime.UtcNow };
        exported.SyncId = Guid.Empty;
        File.WriteAllText(Path.Combine(_folder, "My Resume.json"), JsonSerializer.Serialize(exported));

        var first = await _service.SyncAllAsync();
        var second = await _service.SyncAllAsync();

        first.DownloadedCount.Should().Be(1);
        second.DownloadedCount.Should().Be(0, "the id must be written back so the second sync recognises the file");
        _store.Resumes.Should().ContainSingle(r => r.Name == "Exported before sync ids");
        File.Exists(Path.Combine(_folder, "My Resume.json")).Should().BeFalse();
        Directory.GetFiles(_folder, "*.json").Should().ContainSingle()
            .Which.Should().MatchRegex(@"[0-9a-f-]{36}\.json$");
    }

    [Fact]
    public async Task CloudConflictCopy_DoesNotOverrideTheCanonicalFile()
    {
        await _service.ConfigureAsync(_folder);
        var id = Guid.NewGuid();
        var canonical = new Resume { SyncId = id, Name = "Canonical", UpdatedAt = DateTime.UtcNow.AddMinutes(-5) };
        var copy = new Resume { SyncId = id, Name = "Conflicted copy", UpdatedAt = DateTime.UtcNow };
        File.WriteAllText(Path.Combine(_folder, $"{id:D}.json"), JsonSerializer.Serialize(canonical));
        File.WriteAllText(Path.Combine(_folder, $"{id:D} (conflicted copy).json"), JsonSerializer.Serialize(copy));

        var result = await _service.SyncAllAsync();

        _store.Resumes.Should().ContainSingle().Which.Name.Should().Be("Canonical");
        result.Warnings.Should().ContainSingle().Which.Should().Contain("conflicted copy");
    }

    [Fact]
    public async Task LocalDelete_ParksTheRemoteFile_InsteadOfBringingTheResumeBack()
    {
        await _service.ConfigureAsync(_folder);
        var resume = _store.Seed("Deleted here");
        await _service.SyncAllAsync();

        _store.Resumes.Remove(resume);
        var result = await _service.SyncAllAsync();

        result.RemovedCount.Should().Be(1);
        result.DownloadedCount.Should().Be(0);
        _store.Resumes.Should().BeEmpty();
        File.Exists(Path.Combine(_folder, $"{resume.SyncId:D}.json")).Should().BeFalse();
        File.Exists(Path.Combine(_folder, $"{resume.SyncId:D}.deleted.json")).Should().BeTrue("nothing in a shared folder is destroyed");

        // And a third sync does not resurrect it from the parked file.
        (await _service.SyncAllAsync()).DownloadedCount.Should().Be(0);
    }

    [Fact]
    public async Task RemoteDelete_KeepsTheLocalCopy_AndDoesNotReuploadUntilItIsEdited()
    {
        await _service.ConfigureAsync(_folder);
        var resume = _store.Seed("Deleted elsewhere");
        await _service.SyncAllAsync();
        var path = Path.Combine(_folder, $"{resume.SyncId:D}.json");

        File.Delete(path);
        var afterDelete = await _service.SyncAllAsync();

        afterDelete.UploadedCount.Should().Be(0, "a deletion on the other machine must not come back");
        afterDelete.Warnings.Should().ContainSingle().Which.Should().Contain("Deleted elsewhere");
        _store.Resumes.Should().ContainSingle();
        File.Exists(path).Should().BeFalse();

        (await _service.SyncAllAsync()).UploadedCount.Should().Be(0);

        resume.UpdatedAt = DateTime.UtcNow.AddSeconds(5);
        var afterEdit = await _service.SyncAllAsync();

        afterEdit.UploadedCount.Should().Be(1, "an edit after the deletion is the person choosing to keep it");
        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task SyncingOneResumeExplicitly_OverridesARemoteDeletion()
    {
        await _service.ConfigureAsync(_folder);
        var resume = _store.Seed("Wanted back");
        await _service.SyncAllAsync();
        var path = Path.Combine(_folder, $"{resume.SyncId:D}.json");
        File.Delete(path);
        await _service.SyncAllAsync();

        var result = await _service.SyncResumeAsync(resume.Id);

        result.UploadedCount.Should().Be(1);
        File.Exists(path).Should().BeTrue();
    }

    private sealed class Store : ISyncResumeStore
    {
        private int _nextId = 1;
        public List<Resume> Resumes { get; } = new();

        public Resume Seed(string name)
        {
            var resume = new Resume { Id = _nextId++, Name = name, UpdatedAt = DateTime.UtcNow };
            Resumes.Add(resume);
            return resume;
        }

        public Task<List<Resume>> GetAllAsync() => Task.FromResult(Resumes.ToList());
        public Task<Resume?> GetByIdAsync(int id) => Task.FromResult(Resumes.FirstOrDefault(r => r.Id == id));

        public Task<Resume> CreateAsync(Resume resume)
        {
            resume.Id = _nextId++;
            Resumes.Add(resume);
            return Task.FromResult(resume);
        }

        public Task<Resume> UpdateAsync(Resume resume)
        {
            var index = Resumes.FindIndex(r => r.Id == resume.Id);
            if (index >= 0) Resumes[index] = resume;
            return Task.FromResult(resume);
        }
    }
}
