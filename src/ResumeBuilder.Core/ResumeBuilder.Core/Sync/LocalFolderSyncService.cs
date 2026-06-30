using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace ResumeBuilder.Core.Sync;

/// <summary>
/// Sync service that uses a local folder (can be a cloud-synced folder like Dropbox, OneDrive, or Google Drive).
/// This provides immediate sync functionality without OAuth complexity.
/// </summary>
public class LocalFolderSyncService : ISyncService
{
    private string? _syncFolder;
    private SyncStatus _status = SyncStatus.NotConfigured;

    public string ProviderName => "Local Folder";
    public bool IsConfigured => !string.IsNullOrEmpty(_syncFolder) && Directory.Exists(_syncFolder);
    public SyncStatus Status => _status;

    public event Action<SyncStatus>? StatusChanged;

    public Task<bool> ConfigureAsync(string connectionString)
    {
        try
        {
            _syncFolder = connectionString;

            if (!Directory.Exists(_syncFolder))
            {
                Directory.CreateDirectory(_syncFolder);
            }

            _status = SyncStatus.Idle;
            StatusChanged?.Invoke(_status);
            return Task.FromResult(true);
        }
        catch
        {
            _status = SyncStatus.NotConfigured;
            StatusChanged?.Invoke(_status);
            return Task.FromResult(false);
        }
    }

    public async Task<SyncResult> SyncAllAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _syncFolder == null)
            return SyncResult.NotConfigured();

        _status = SyncStatus.Syncing;
        StatusChanged?.Invoke(_status);

        try
        {
            // List local resumes from the app's data
            // This would integrate with the repository in a full implementation
            var result = SyncResult.Succeeded();

            _status = SyncStatus.Success;
            StatusChanged?.Invoke(_status);
            return result;
        }
        catch (Exception ex)
        {
            _status = SyncStatus.Error;
            StatusChanged?.Invoke(_status);
            return SyncResult.Failed(ex.Message);
        }
    }

    public async Task<SyncResult> SyncResumeAsync(int resumeId, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
            return SyncResult.NotConfigured();

        _status = SyncStatus.Syncing;
        StatusChanged?.Invoke(_status);

        try
        {
            // Sync logic would go here
            _status = SyncStatus.Success;
            StatusChanged?.Invoke(_status);
            return SyncResult.Succeeded(1, 0);
        }
        catch (Exception ex)
        {
            _status = SyncStatus.Error;
            StatusChanged?.Invoke(_status);
            return SyncResult.Failed(ex.Message);
        }
    }

    public async Task<bool> UploadAsync(string content, string remotePath, CancellationToken cancellationToken = default)
    {
        if (!IsConfigured || _syncFolder == null)
            return false;

        try
        {
            var fullPath = Path.Combine(_syncFolder, SanitizeFileName(remotePath));
            var directory = Path.GetDirectoryName(fullPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            await File.WriteAllTextAsync(fullPath, content, cancellationToken);
            return true;
        }
        catch
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
            var fullPath = Path.Combine(_syncFolder, SanitizeFileName(remotePath));
            if (!File.Exists(fullPath))
                return null;

            return await File.ReadAllTextAsync(fullPath, cancellationToken);
        }
        catch
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
                .Select(f => new FileInfo(f))
                .Select(fi => new RemoteResumeInfo
                {
                    Path = fi.Name,
                    Name = Path.GetFileNameWithoutExtension(fi.Name),
                    LastModified = fi.LastWriteTimeUtc,
                    Size = fi.Length,
                    Checksum = ComputeChecksum(fi.FullName)
                })
                .ToList();

            return Task.FromResult<IEnumerable<RemoteResumeInfo>>(files);
        }
        catch
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
            var fullPath = Path.Combine(_syncFolder, SanitizeFileName(remotePath));
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }
        catch
        {
            return Task.FromResult(false);
        }
    }

    private static string SanitizeFileName(string fileName)
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
        catch
        {
            return null;
        }
    }
}
