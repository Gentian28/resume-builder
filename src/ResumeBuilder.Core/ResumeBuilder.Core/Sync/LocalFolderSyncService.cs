using System.Security.Cryptography;
using System.Text.Json;
using ResumeBuilder.Core.Models;

namespace ResumeBuilder.Core.Sync;

/// <summary>How to settle a resume that changed on both sides since the last sync.</summary>
public enum ConflictResolution
{
    /// <summary>Keep whichever side has the newer UpdatedAt. Both versions are preserved:
    /// the loser is written next to the winner as a <c>.conflict.json</c> file.</summary>
    NewestWins,

    /// <summary>Always keep the local version.</summary>
    PreferLocal,

    /// <summary>Always keep the remote version.</summary>
    PreferRemote,

    /// <summary>Change nothing and report the conflict.</summary>
    Skip
}

/// <summary>
/// Sync service that uses a local folder (can be a cloud-synced folder like Dropbox, OneDrive, or Google Drive).
/// This provides immediate sync functionality without OAuth complexity.
/// </summary>
public class LocalFolderSyncService : ISyncService
{
    private static readonly JsonSerializerOptions SerializerOptions = new() { WriteIndented = true };

    private readonly ISyncResumeStore? _store;
    private readonly SyncStateStore _state;
    private string? _syncFolder;
    private SyncStatus _status = SyncStatus.NotConfigured;

    public LocalFolderSyncService(ISyncResumeStore? store = null, SyncStateStore? state = null)
    {
        _store = store;
        _state = state ?? new SyncStateStore();
    }

    public string ProviderName => "Local Folder";
    public bool IsConfigured => !string.IsNullOrEmpty(_syncFolder) && Directory.Exists(_syncFolder);
    public SyncStatus Status => _status;

    /// <summary>How two-way conflicts are settled. Defaults to newest-wins with a backup of the loser.</summary>
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.NewestWins;

    public event Action<SyncStatus>? StatusChanged;

    public async Task<bool> ConfigureAsync(string connectionString)
    {
        try
        {
            _syncFolder = connectionString;

            if (!Directory.Exists(_syncFolder))
            {
                Directory.CreateDirectory(_syncFolder);
            }

            await _state.LoadAsync();

            SetStatus(SyncStatus.Idle);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException)
        {
            _syncFolder = null;
            SetStatus(SyncStatus.NotConfigured);
            return false;
        }
    }

    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _syncFolder == null)
            return SyncResult.NotConfigured();

        if (_store == null)
            return SyncResult.Failed("Sync has no resume store attached, so there is nothing to sync.");

        SetStatus(SyncStatus.Syncing);

        try
        {
            var locals = await _store.GetAllAsync();
            var remotes = await ReadRemoteResumesAsync(cancellationToken);

            var uploaded = 0;
            var downloaded = 0;
            var removed = 0;
            var errors = new List<string>();
            var warnings = new List<string>(remotes.Warnings);
            var conflicts = new List<SyncConflict>();

            // The state store is the third party to the union: a resume it knows that is now
            // missing on one side was deleted there, which is not the same as never having existed.
            var syncIds = locals.Select(r => r.SyncId)
                .Union(remotes.Files.Keys)
                .Union(_state.KnownIds)
                .ToList();

            foreach (var syncId in syncIds)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var local = locals.FirstOrDefault(r => r.SyncId == syncId);
                remotes.Files.TryGetValue(syncId, out var remote);

                try
                {
                    var outcome = await SyncOneAsync(local, remote, cancellationToken);
                    uploaded += outcome.Uploaded;
                    downloaded += outcome.Downloaded;
                    removed += outcome.Removed;
                    if (outcome.Warning != null)
                    {
                        warnings.Add(outcome.Warning);
                    }
                    if (outcome.Conflict != null)
                    {
                        conflicts.Add(outcome.Conflict);
                    }
                }
                catch (Exception ex) when (ex is IOException or JsonException or InvalidOperationException)
                {
                    errors.Add($"{local?.Name ?? remote?.Resume.Name ?? syncId.ToString()}: {ex.Message}");
                }
            }

            await _state.SaveAsync();

            var result = new SyncResult
            {
                Success = errors.Count == 0,
                Status = errors.Count > 0 ? SyncStatus.Error
                    : conflicts.Count > 0 ? SyncStatus.Conflict
                    : SyncStatus.Success,
                UploadedCount = uploaded,
                DownloadedCount = downloaded,
                RemovedCount = removed,
                Errors = errors,
                Warnings = warnings,
                Conflicts = conflicts,
                Message = BuildMessage(uploaded, downloaded, removed, conflicts.Count, errors.Count)
            };

            SetStatus(result.Status);
            return result;
        }
        catch (OperationCanceledException)
        {
            SetStatus(SyncStatus.Idle);
            return SyncResult.Failed("Sync cancelled.");
        }
        catch (Exception ex)
        {
            SetStatus(SyncStatus.Error);
            return SyncResult.Failed(ex.Message);
        }
    }

    public async Task<SyncResult> SyncResumeAsync(int resumeId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _syncFolder == null)
            return SyncResult.NotConfigured();

        if (_store == null)
            return SyncResult.Failed("Sync has no resume store attached, so there is nothing to sync.");

        SetStatus(SyncStatus.Syncing);

        try
        {
            var local = await _store.GetByIdAsync(resumeId);
            if (local == null)
            {
                SetStatus(SyncStatus.Error);
                return SyncResult.Failed($"Resume {resumeId} not found.");
            }

            var remotes = await ReadRemoteResumesAsync(cancellationToken);
            remotes.Files.TryGetValue(local.SyncId, out var remote);

            // An explicit "sync this one" is the user overriding a remote deletion on purpose.
            _state.Remove(local.SyncId);
            var outcome = await SyncOneAsync(local, remote, cancellationToken);
            await _state.SaveAsync();

            var result = new SyncResult
            {
                Success = true,
                Status = outcome.Conflict != null ? SyncStatus.Conflict : SyncStatus.Success,
                UploadedCount = outcome.Uploaded,
                DownloadedCount = outcome.Downloaded,
                Warnings = remotes.Warnings.ToList(),
                Conflicts = outcome.Conflict != null ? new List<SyncConflict> { outcome.Conflict } : new List<SyncConflict>(),
                Message = BuildMessage(outcome.Uploaded, outcome.Downloaded, 0, outcome.Conflict != null ? 1 : 0, 0)
            };

            SetStatus(result.Status);
            return result;
        }
        catch (Exception ex)
        {
            SetStatus(SyncStatus.Error);
            return SyncResult.Failed(ex.Message);
        }
    }

    private readonly record struct SyncOutcome(int Uploaded, int Downloaded, int Removed, SyncConflict? Conflict, string? Warning)
    {
        public SyncOutcome(int uploaded, int downloaded, SyncConflict? conflict) : this(uploaded, downloaded, 0, conflict, null) { }
    }

    private async Task<SyncOutcome> SyncOneAsync(Resume? local, RemoteResume? remote, CancellationToken cancellationToken)
    {
        var known = local != null ? _state.Get(local.SyncId) : remote != null ? _state.Get(remote.Resume.SyncId) : null;

        // Only on one side. Whether that means "new" or "deleted" depends on whether this machine
        // has synced it before; without the state, a deletion on either side came straight back.
        if (local != null && remote == null)
        {
            if (known?.DeletedAt is { } deletedAt)
            {
                if (local.UpdatedAt <= deletedAt)
                    return new SyncOutcome(0, 0, null);

                // Edited after the other machine removed it: the edit wins and the file returns.
                await WriteRemoteAsync(local, cancellationToken);
                return new SyncOutcome(1, 0, null);
            }

            if (known?.LastSyncedAt != null)
            {
                _state.MarkRemoteDeleted(local.SyncId, DateTime.UtcNow);
                return new SyncOutcome(0, 0, 0, null,
                    $"\"{local.Name}\" was removed from the sync folder on another machine. It is kept here and not sent again unless you edit it.");
            }

            await WriteRemoteAsync(local, cancellationToken);
            return new SyncOutcome(1, 0, null);
        }

        if (local == null && remote != null)
        {
            if (known?.LastSyncedAt != null)
            {
                // Deleted on this machine after a sync: park the file rather than bringing it back.
                ParkDeleted(remote);
                _state.Remove(remote.Resume.SyncId);
                return new SyncOutcome(0, 0, 1, null, null);
            }

            await SaveDownloadedAsync(remote.Resume, existingLocalId: null);
            RecordSynced(remote.Resume.SyncId, remote.Checksum);
            return new SyncOutcome(0, 1, null);
        }

        if (local == null || remote == null)
        {
            // Gone on both sides: nothing left to remember.
            if (known != null)
            {
                _state.Remove(Guid.Parse(known.SyncId!));
            }
            return new SyncOutcome(0, 0, null);
        }

        var metadata = _state.Get(local.SyncId);
        var localChanged = metadata?.LastSyncedAt == null || local.UpdatedAt > metadata.LastSyncedAt;
        var remoteChanged = metadata?.RemoteChecksum == null || remote.Checksum != metadata.RemoteChecksum;

        if (!localChanged && !remoteChanged)
        {
            return new SyncOutcome(0, 0, null);
        }

        if (localChanged && !remoteChanged)
        {
            await WriteRemoteAsync(local, cancellationToken);
            return new SyncOutcome(1, 0, null);
        }

        if (!localChanged && remoteChanged)
        {
            await SaveDownloadedAsync(remote.Resume, local.Id);
            RecordSynced(local.SyncId, remote.Checksum);
            return new SyncOutcome(0, 1, null);
        }

        // Both sides changed since the last sync.
        var conflict = new SyncConflict
        {
            ResumeId = local.SyncId.ToString(),
            ResumeName = local.Name,
            LocalModified = local.UpdatedAt,
            RemoteModified = remote.Resume.UpdatedAt
        };

        var keepLocal = ConflictResolution switch
        {
            ConflictResolution.PreferLocal => true,
            ConflictResolution.PreferRemote => false,
            ConflictResolution.NewestWins => local.UpdatedAt >= remote.Resume.UpdatedAt,
            _ => (bool?)null
        };

        if (keepLocal == null)
        {
            return new SyncOutcome(0, 0, conflict);
        }

        if (keepLocal.Value)
        {
            // Never destroy the loser: park the remote version alongside it before overwriting.
            await BackupConflictAsync(remote, cancellationToken);
            await WriteRemoteAsync(local, cancellationToken);
            return new SyncOutcome(1, 0, conflict);
        }

        await BackupConflictLocalAsync(local, cancellationToken);
        await SaveDownloadedAsync(remote.Resume, local.Id);
        RecordSynced(local.SyncId, remote.Checksum);
        return new SyncOutcome(0, 1, conflict);
    }

    private async Task SaveDownloadedAsync(Resume incoming, int? existingLocalId)
    {
        incoming.SectionOrder.EnsureAllSectionsPresent();

        if (existingLocalId is null)
        {
            incoming.Id = 0;
            await _store!.CreateAsync(incoming);
            return;
        }

        // Reuse the local row for this SyncId; the remote file's Id belongs to another database.
        var current = await _store!.GetByIdAsync(existingLocalId.Value);
        incoming.Id = existingLocalId.Value;
        incoming.RowVersion = current?.RowVersion ?? Guid.Empty;
        await _store.UpdateAsync(incoming);
    }

    private void RecordSynced(Guid syncId, string? checksum)
    {
        _state.Set(syncId, DateTime.UtcNow, checksum);
    }

    private async Task WriteRemoteAsync(Resume resume, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(resume, SerializerOptions);
        var path = RemotePathFor(resume.SyncId);
        await AtomicFile.WriteAllTextAsync(path, json, cancellationToken);
        RecordSynced(resume.SyncId, ComputeChecksum(path));
    }

    private async Task BackupConflictAsync(RemoteResume remote, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(remote.Resume, SerializerOptions);
        var path = Path.Combine(_syncFolder!, $"{remote.Resume.SyncId:D}.remote.conflict.json");
        await AtomicFile.WriteAllTextAsync(path, json, cancellationToken);
    }

    private async Task BackupConflictLocalAsync(Resume local, CancellationToken cancellationToken)
    {
        var json = JsonSerializer.Serialize(local, SerializerOptions);
        var path = Path.Combine(_syncFolder!, $"{local.SyncId:D}.local.conflict.json");
        await AtomicFile.WriteAllTextAsync(path, json, cancellationToken);
    }

    /// <summary>
    /// A resume deleted on this machine is not deleted from the folder; its file is renamed so the
    /// other machine stops seeing it and nothing is destroyed. Cloud folders are shared with a
    /// person who may not have meant it.
    /// </summary>
    private void ParkDeleted(RemoteResume remote)
    {
        var parked = Path.Combine(_syncFolder!, $"{remote.Resume.SyncId:D}.deleted.json");
        File.Move(remote.Path, parked, overwrite: true);
    }

    private string RemotePathFor(Guid syncId) => Path.Combine(_syncFolder!, $"{syncId:D}.json");

    private static bool IsCanonicalPath(string path, Guid syncId) =>
        string.Equals(Path.GetFileName(path), $"{syncId:D}.json", StringComparison.OrdinalIgnoreCase);

    private sealed record RemoteResume(Resume Resume, string Path, string? Checksum);

    private sealed record RemoteFolder(Dictionary<Guid, RemoteResume> Files, List<string> Warnings);

    private async Task<RemoteFolder> ReadRemoteResumesAsync(CancellationToken cancellationToken)
    {
        var map = new Dictionary<Guid, RemoteResume>();
        var warnings = new List<string>();

        foreach (var file in Directory.GetFiles(_syncFolder!, "*.json"))
        {
            cancellationToken.ThrowIfCancellationRequested();

            // Conflict backups and parked deletions are archives, not sync participants.
            if (file.EndsWith(".conflict.json", StringComparison.OrdinalIgnoreCase)
                || file.EndsWith(".deleted.json", StringComparison.OrdinalIgnoreCase))
                continue;

            try
            {
                var json = File.ReadAllText(file);
                var resume = JsonSerializer.Deserialize<Resume>(json);
                if (resume == null)
                    continue;

                var path = file;
                if (resume.SyncId == Guid.Empty)
                {
                    // A resume exported before sync ids existed. Adopt the filename if it is a guid,
                    // otherwise mint one. Either way the file is rewritten under that id, because an
                    // id that lives only in memory is minted again next time and the resume imports
                    // again as a new record on every sync.
                    resume.SyncId = Guid.TryParse(Path.GetFileNameWithoutExtension(file), out var fromName)
                        ? fromName
                        : Guid.NewGuid();
                    path = RemotePathFor(resume.SyncId);
                    await AtomicFile.WriteAllTextAsync(path, JsonSerializer.Serialize(resume, SerializerOptions), cancellationToken);
                    if (!string.Equals(path, file, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                    }
                }

                // Cloud clients write "name (conflicted copy).json" next to the real file when both
                // machines changed it. Both parse, both carry the same id, and whichever the
                // directory listing returned last used to win. The file named after the id is the
                // one the sync wrote; the other is left for the person to look at.
                if (map.TryGetValue(resume.SyncId, out var existing))
                {
                    var keep = IsCanonicalPath(path, resume.SyncId) ? path : existing.Path;
                    var ignore = ReferenceEquals(keep, path) ? existing.Path : path;
                    warnings.Add($"\"{Path.GetFileName(ignore)}\" carries the same id as \"{Path.GetFileName(keep)}\" and was ignored. Compare them by hand if it holds work you want.");
                    if (!ReferenceEquals(keep, path))
                        continue;
                }

                map[resume.SyncId] = new RemoteResume(resume, path, ComputeChecksum(path));
            }
            catch (Exception ex) when (ex is IOException or JsonException)
            {
                // Not a resume file (or unreadable) - ignore it rather than failing the whole sync.
            }
        }

        return new RemoteFolder(map, warnings);
    }

    private static string BuildMessage(int uploaded, int downloaded, int removed, int conflicts, int errors)
    {
        var parts = new List<string>();
        if (uploaded > 0) parts.Add($"{uploaded} uploaded");
        if (downloaded > 0) parts.Add($"{downloaded} downloaded");
        if (removed > 0) parts.Add($"{removed} removed");
        if (conflicts > 0) parts.Add($"{conflicts} conflict{(conflicts == 1 ? "" : "s")} resolved");
        if (errors > 0) parts.Add($"{errors} failed");
        return parts.Count == 0 ? "Already up to date" : string.Join(", ", parts);
    }

    private void SetStatus(SyncStatus status)
    {
        _status = status;
        StatusChanged?.Invoke(status);
    }

    public async Task<bool> UploadAsync(string content, string remotePath, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _syncFolder == null)
            return false;

        try
        {
            var fullPath = ResolveRemotePath(remotePath);
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content, cancellationToken);
            return true;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public async Task<string?> DownloadAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _syncFolder == null)
            return null;

        try
        {
            var fullPath = ResolveRemotePath(remotePath);
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllTextAsync(fullPath, cancellationToken);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public Task<IEnumerable<RemoteResumeInfo>> ListRemoteAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _syncFolder == null)
            return Task.FromResult(Enumerable.Empty<RemoteResumeInfo>());

        try
        {
            var files = Directory.GetFiles(_syncFolder, "*.json")
                .Where(f => !f.EndsWith(".conflict.json", StringComparison.OrdinalIgnoreCase))
                .Select(f => new FileInfo(f))
                .Select(fi => new RemoteResumeInfo
                {
                    Path = fi.Name,
                    Name = ReadRemoteName(fi.FullName) ?? Path.GetFileNameWithoutExtension(fi.Name),
                    LastModified = fi.LastWriteTimeUtc,
                    Size = fi.Length,
                    Checksum = ComputeChecksum(fi.FullName)
                })
                .ToList();

            return Task.FromResult<IEnumerable<RemoteResumeInfo>>(files);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(Enumerable.Empty<RemoteResumeInfo>());
        }
    }

    public Task<bool> DeleteRemoteAsync(string remotePath, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _syncFolder == null)
            return Task.FromResult(false);

        try
        {
            var fullPath = ResolveRemotePath(remotePath);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return Task.FromResult(false);
        }
    }

    private static string? ReadRemoteName(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            return document.RootElement.TryGetProperty("Name", out var name) ? name.GetString() : null;
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Maps a remote path onto the sync folder, sanitizing each segment. Sanitizing the whole path
    /// at once would strip the separators and flatten any subfolder into the file name.
    /// </summary>
    private string ResolveRemotePath(string remotePath)
    {
        var segments = remotePath
            .Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(SanitizeFileName)
            .Where(s => s.Length > 0 && s != "." && s != "..")
            .ToArray();

        if (segments.Length == 0)
        {
            throw new ArgumentException("Remote path is empty after sanitization.", nameof(remotePath));
        }

        return Path.Combine(new[] { _syncFolder! }.Concat(segments).ToArray());
    }

    public static string SanitizeFileName(string fileName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        return string.Join("_", fileName.Split(invalid, StringSplitOptions.RemoveEmptyEntries));
    }

    private static string? ComputeChecksum(string filePath)
    {
        try
        {
            using var md5 = MD5.Create();
            using var stream = File.OpenRead(filePath);
            var hash = md5.ComputeHash(stream);
            return Convert.ToHexString(hash);
        }
        catch (IOException)
        {
            return null;
        }
    }
}
