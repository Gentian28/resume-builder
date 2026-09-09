namespace ResumeBuilder.Core.Sync;

/// <summary>
/// Writes a file so that no reader ever sees it half-written.
///
/// The sync folder is watched by a cloud client that uploads on every change, and by the other
/// machine that downloads what the client uploaded. Writing straight onto the target means a
/// truncated file is a real state both of them can observe, and a crash mid-write leaves one
/// behind for good. Writing the whole content next to the target and then renaming makes the
/// switch a single filesystem operation.
/// </summary>
public static class AtomicFile
{
    public const string TemporarySuffix = ".tmp";

    public static async Task WriteAllTextAsync(string path, string content, CancellationToken cancellationToken = default)
    {
        var temporary = path + TemporarySuffix;
        try
        {
            await File.WriteAllTextAsync(temporary, content, cancellationToken);
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                try { File.Delete(temporary); } catch (IOException) { }
            }
        }
    }
}
