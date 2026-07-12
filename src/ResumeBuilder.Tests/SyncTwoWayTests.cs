using FluentAssertions;
using ResumeBuilder.Core.Models;
using ResumeBuilder.Core.Sync;

namespace ResumeBuilder.Tests;

/// <summary>
/// Covers the two-way sync that previously reported success while moving no data at all.
/// </summary>
public class SyncTwoWayTests : IDisposable
{
    private readonly string _folder;
    private readonly string _statePath;
    private readonly FakeResumeStore _store = new();
    private readonly LocalFolderSyncService _service;

    public SyncTwoWayTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), $"SyncTest_{Guid.NewGuid()}");
        _statePath = Path.Combine(_folder, "state", "sync-state.json");
        _service = new LocalFolderSyncService(_store, new SyncStateStore(_statePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    [Fact]
    public async Task SyncAll_WithoutStore_DoesNotClaimSuccess()
    {
        var service = new LocalFolderSyncService(store: null, new SyncStateStore(_statePath));
        await service.ConfigureAsync(_folder);

        var result = await service.SyncAllAsync();

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("store");
    }

    [Fact]
    public async Task SyncAll_LocalOnlyResume_UploadsIt()
    {
        await _service.ConfigureAsync(_folder);
        var resume = _store.Seed("My Resume");

        var result = await _service.SyncAllAsync();

        result.Success.Should().BeTrue();
        result.UploadedCount.Should().Be(1);
        File.Exists(Path.Combine(_folder, $"{resume.SyncId:D}.json")).Should().BeTrue();
    }

    [Fact]
    public async Task SyncAll_RemoteOnlyResume_DownloadsItAsNewLocalRecord()
    {
        await _service.ConfigureAsync(_folder);

        var remote = new Resume { Id = 77, Name = "From Other Machine", UpdatedAt = DateTime.UtcNow };
        WriteRemote(remote);

        var result = await _service.SyncAllAsync();

        result.DownloadedCount.Should().Be(1);
        _store.Resumes.Should().ContainSingle(r => r.Name == "From Other Machine");

        // The remote file's Id belongs to another database and must not be reused verbatim.
        _store.Resumes.Single().Id.Should().NotBe(77);
    }

    [Fact]
    public async Task SyncAll_UnchangedOnBothSides_DoesNothingOnSecondRun()
    {
        await _service.ConfigureAsync(_folder);
        _store.Seed("Stable");

        await _service.SyncAllAsync();
        var second = await _service.SyncAllAsync();

        second.UploadedCount.Should().Be(0);
        second.DownloadedCount.Should().Be(0);
        second.Message.Should().Be("Already up to date");
    }

    [Fact]
    public async Task SyncAll_RemoteChangedOnly_UpdatesLocalInPlace()
    {
        await _service.ConfigureAsync(_folder);
        var local = _store.Seed("Original");
        await _service.SyncAllAsync();

        var edited = Clone(local);
        edited.Name = "Edited Remotely";
        edited.UpdatedAt = DateTime.UtcNow.AddMinutes(5);
        WriteRemote(edited);

        var result = await _service.SyncAllAsync();

        result.DownloadedCount.Should().Be(1);
        _store.Resumes.Should().ContainSingle();
        _store.Resumes.Single().Id.Should().Be(local.Id);
        _store.Resumes.Single().Name.Should().Be("Edited Remotely");
    }

    [Fact]
    public async Task SyncAll_BothSidesChanged_ReportsConflictAndKeepsNewest()
    {
        await _service.ConfigureAsync(_folder);
        var local = _store.Seed("Base");
        await _service.SyncAllAsync();

        // Remote edited...
        var remoteEdit = Clone(local);
        remoteEdit.Name = "Remote Wins";
        remoteEdit.UpdatedAt = DateTime.UtcNow.AddMinutes(10);
        WriteRemote(remoteEdit);

        // ...and local edited too, but less recently.
        local.Name = "Local Edit";
        local.UpdatedAt = DateTime.UtcNow.AddMinutes(1);

        var result = await _service.SyncAllAsync();

        result.Conflicts.Should().ContainSingle();
        result.Status.Should().Be(SyncStatus.Conflict);
        _store.Resumes.Single().Name.Should().Be("Remote Wins");

        // The losing version is preserved, never silently destroyed.
        Directory.GetFiles(_folder, "*.local.conflict.json").Should().ContainSingle();
    }

    [Fact]
    public async Task SyncAll_BothSidesChanged_SkipPolicy_ChangesNothing()
    {
        await _service.ConfigureAsync(_folder);
        _service.ConflictResolution = ConflictResolution.Skip;

        var local = _store.Seed("Base");
        await _service.SyncAllAsync();

        var remoteEdit = Clone(local);
        remoteEdit.Name = "Remote Edit";
        remoteEdit.UpdatedAt = DateTime.UtcNow.AddMinutes(10);
        WriteRemote(remoteEdit);

        local.Name = "Local Edit";
        local.UpdatedAt = DateTime.UtcNow.AddMinutes(1);

        var result = await _service.SyncAllAsync();

        result.Conflicts.Should().ContainSingle();
        _store.Resumes.Single().Name.Should().Be("Local Edit");
    }

    private void WriteRemote(Resume resume)
    {
        var json = System.Text.Json.JsonSerializer.Serialize(resume);
        File.WriteAllText(Path.Combine(_folder, $"{resume.SyncId:D}.json"), json);
    }

    private static Resume Clone(Resume resume) =>
        System.Text.Json.JsonSerializer.Deserialize<Resume>(System.Text.Json.JsonSerializer.Serialize(resume))!;

    private sealed class FakeResumeStore : ISyncResumeStore
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

        public Task<Resume?> GetByIdAsync(int id) =>
            Task.FromResult(Resumes.FirstOrDefault(r => r.Id == id));

        public Task<Resume> CreateAsync(Resume resume)
        {
            resume.Id = _nextId++;
            Resumes.Add(resume);
            return Task.FromResult(resume);
        }

        public Task<Resume> UpdateAsync(Resume resume)
        {
            var index = Resumes.FindIndex(r => r.Id == resume.Id);
            if (index >= 0)
            {
                Resumes[index] = resume;
            }
            return Task.FromResult(resume);
        }
    }
}
