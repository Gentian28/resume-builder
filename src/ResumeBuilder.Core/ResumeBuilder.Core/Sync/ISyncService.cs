namespace ResumeBuilder.Core.Sync;

/// <summary>
/// Status of a sync operation.
/// </summary>
public enum SyncStatus
{
    NotConfigured,
    Idle,
    Syncing,
    Success,
    Error,
    Conflict
}

/// <summary>
/// Result of a sync operation.
/// </summary>
public class SyncResult
{
    public bool Success { get; init; }
    public SyncStatus Status { get; init; }
    public string? Message { get; init; }
    public int UploadedCount { get; init; }
    public int DownloadedCount { get; init; }
    public List<string> Errors { get; init; } = new();
    public List<SyncConflict> Conflicts { get; init; } = new();

    public static SyncResult Succeeded(int uploaded = 0, int downloaded = 0)
        => new() { Success = true, Status = SyncStatus.Success, UploadedCount = uploaded, DownloadedCount = downloaded };

    public static SyncResult Failed(string message)
        => new() { Success = false, Status = SyncStatus.Error, Message = message };

    public static SyncResult NotConfigured()
        => new() { Success = false, Status = SyncStatus.NotConfigured, Message = "Sync not configured" };
}

/// <summary>
/// Represents a sync conflict.
/// </summary>
public class SyncConflict
{
    public required string ResumeId { get; init; }
    public required string ResumeName { get; init; }
    public DateTime LocalModified { get; init; }
    public DateTime RemoteModified { get; init; }
}

/// <summary>
/// Metadata for sync tracking.
/// </summary>
public class SyncMetadata
{
    public string? SyncId { get; set; }
    public DateTime? LastSyncedAt { get; set; }
    public int Version { get; set; } = 1;
    public string? RemoteChecksum { get; set; }
}

/// <summary>
/// Interface for sync service implementations.
/// </summary>
public interface ISyncService
{
    /// <summary>
    /// Name of the sync provider (e.g., "Local", "Google Drive", "OneDrive").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Whether the service is configured and ready to sync.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Current sync status.
    /// </summary>
    SyncStatus Status { get; }

    /// <summary>
    /// Event raised when sync status changes.
    /// </summary>
    event Action<SyncStatus>? StatusChanged;

    /// <summary>
    /// Configure the sync service.
    /// </summary>
    Task<bool> ConfigureAsync(string connectionString);

    /// <summary>
    /// Sync all resumes.
    /// </summary>
    Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Sync a specific resume.
    /// </summary>
    Task<SyncResult> SyncResumeAsync(int resumeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload a resume to the remote location.
    /// </summary>
    Task<bool> UploadAsync(string content, string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// Download a resume from the remote location.
    /// </summary>
    Task<string?> DownloadAsync(string remotePath, CancellationToken cancellationToken = default);

    /// <summary>
    /// List all remote resumes.
    /// </summary>
    Task<IEnumerable<RemoteResumeInfo>> ListRemoteAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a resume from the remote location.
    /// </summary>
    Task<bool> DeleteRemoteAsync(string remotePath, CancellationToken cancellationToken = default);
}

/// <summary>
/// Information about a remote resume file.
/// </summary>
public class RemoteResumeInfo
{
    public required string Path { get; init; }
    public required string Name { get; init; }
    public DateTime LastModified { get; init; }
    public long Size { get; init; }
    public string? Checksum { get; init; }
}
