using System.Text.Json;

namespace ResumeBuilder.Core.Sync;

/// <summary>
/// Remembers what each resume looked like at the end of the last sync. Without this, "both sides
/// changed" is indistinguishable from "one side changed", and a two-way sync can only guess.
/// </summary>
public class SyncStateStore
{
    private readonly string _statePath;
    private Dictionary<Guid, SyncMetadata> _entries = new();

    public SyncStateStore(string? statePath = null)
    {
        _statePath = statePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ResumeBuilder",
            "sync-state.json");
    }

    public SyncMetadata? Get(Guid syncId) =>
        _entries.TryGetValue(syncId, out var metadata) ? metadata : null;

    public void Set(Guid syncId, DateTime syncedAt, string? remoteChecksum)
    {
        _entries[syncId] = new SyncMetadata
        {
            SyncId = syncId.ToString(),
            LastSyncedAt = syncedAt,
            RemoteChecksum = remoteChecksum,
            Version = (Get(syncId)?.Version ?? 0) + 1
        };
    }

    public void Remove(Guid syncId) => _entries.Remove(syncId);

    public async Task LoadAsync()
    {
        try
        {
            if (!File.Exists(_statePath))
            {
                _entries = new Dictionary<Guid, SyncMetadata>();
                return;
            }

            var json = await File.ReadAllTextAsync(_statePath);
            _entries = JsonSerializer.Deserialize<Dictionary<Guid, SyncMetadata>>(json)
                       ?? new Dictionary<Guid, SyncMetadata>();
        }
        catch (Exception ex) when (ex is IOException or JsonException)
        {
            _entries = new Dictionary<Guid, SyncMetadata>();
        }
    }

    public async Task SaveAsync()
    {
        try
        {
            var directory = Path.GetDirectoryName(_statePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(_entries, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(_statePath, json);
        }
        catch (IOException)
        {
            // Sync state is a cache; losing it only costs us one extra conflict prompt.
        }
    }
}
