namespace ResumeBuilder.Core.Sync;

/// <summary>A cloud-storage folder found on this machine.</summary>
public record CloudFolder(string Name, string Path);

/// <summary>
/// Finds Drive / OneDrive / Dropbox / iCloud folders that already exist locally.
///
/// Sync is the app's answer to "where do my résumés live?", but the panel asks for a folder path,
/// which is a question most people cannot answer without going to look. Detecting the folders that
/// are already mounted turns that into picking a name off a list — the same outcome as a native
/// cloud integration, without an OAuth flow, a client secret in a desktop binary, or the app
/// holding a credential to someone's Drive.
/// </summary>
public static class CloudFolders
{
    /// <summary>
    /// Every provider folder present on this machine, most-likely-intended first. Empty when none
    /// are installed — callers fall back to the folder picker.
    /// </summary>
    public static IReadOnlyList<CloudFolder> Detect()
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var found = new List<CloudFolder>();

        void Add(string name, string? path)
        {
            if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
                return;
            // Drive for desktop can surface the same folder twice (mounted letter and profile
            // path); the first spelling wins.
            if (found.Any(f => string.Equals(f.Path, path, StringComparison.OrdinalIgnoreCase)))
                return;
            found.Add(new CloudFolder(name, path));
        }

        if (OperatingSystem.IsWindows())
        {
            // Drive for desktop mounts a drive letter whose root holds "My Drive". The letter is
            // configurable, so probe rather than assume G:.
            foreach (var drive in SafeDrives())
            {
                Add("Google Drive", System.IO.Path.Combine(drive, "My Drive"));
            }
            Add("Google Drive", System.IO.Path.Combine(home, "Google Drive"));
            Add("Google Drive", System.IO.Path.Combine(home, "My Drive"));

            // OneDrive exports its own variables; personal and work accounts differ.
            Add("OneDrive", Environment.GetEnvironmentVariable("OneDrive"));
            Add("OneDrive", Environment.GetEnvironmentVariable("OneDriveConsumer"));
            Add("OneDrive", Environment.GetEnvironmentVariable("OneDriveCommercial"));
            Add("OneDrive", System.IO.Path.Combine(home, "OneDrive"));

            Add("Dropbox", System.IO.Path.Combine(home, "Dropbox"));
        }
        else if (OperatingSystem.IsMacOS())
        {
            Add("iCloud Drive", System.IO.Path.Combine(home, "Library", "Mobile Documents", "com~apple~CloudDocs"));
            Add("Google Drive", System.IO.Path.Combine(home, "Google Drive"));
            Add("Google Drive", "/Volumes/GoogleDrive/My Drive");
            Add("Dropbox", System.IO.Path.Combine(home, "Dropbox"));
            Add("OneDrive", System.IO.Path.Combine(home, "OneDrive"));
        }
        else
        {
            // Linux has no first-party clients; these are the paths the common third-party ones
            // (rclone, insync, the official Dropbox daemon) default to.
            Add("Google Drive", System.IO.Path.Combine(home, "Google Drive"));
            Add("Dropbox", System.IO.Path.Combine(home, "Dropbox"));
            Add("OneDrive", System.IO.Path.Combine(home, "OneDrive"));
        }

        return found;
    }

    /// <summary>
    /// Suggests where inside a cloud folder the résumés should go, so the user is not asked to
    /// invent a folder name and does not end up syncing to the root of their Drive.
    /// </summary>
    public static string SuggestSyncPath(CloudFolder folder) =>
        System.IO.Path.Combine(folder.Path, "Resume Builder");

    /// <summary>
    /// Enumerating drives throws on a disconnected network mapping, and an unreadable drive must
    /// not stop the others being offered.
    /// </summary>
    private static IEnumerable<string> SafeDrives()
    {
        DriveInfo[] drives;
        try
        {
            drives = DriveInfo.GetDrives();
        }
        catch (IOException)
        {
            yield break;
        }

        foreach (var drive in drives)
        {
            string root;
            try
            {
                if (!drive.IsReady) continue;
                root = drive.RootDirectory.FullName;
            }
            catch (IOException)
            {
                continue;
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }

            yield return root;
        }
    }
}
