using Microsoft.EntityFrameworkCore;

namespace ResumeBuilder.Data;

/// <summary>
/// Takes a copy of the database before the schema is changed, and keeps the last few.
///
/// Schema upgrades are the one moment this app rewrites a file holding work the user cannot
/// reproduce: an employment history is not something you retype from memory. There are no EF
/// migrations here, so an upgrade is hand-written steps against a live database; a mistake in
/// <see cref="DatabaseInitializer.AddedColumns"/> is discovered on a user's machine, not in CI.
/// A copy costs milliseconds and makes that recoverable instead of terminal.
/// </summary>
public static class DatabaseBackup
{
    /// <summary>Enough to survive a bad upgrade that is only noticed a version or two later.</summary>
    public const int KeepCount = 3;

    /// <summary>
    /// Copies the database next to itself as <c>resumes.backup-{timestamp}.db</c>, pruning older
    /// copies. The result says which of three things happened: a copy was made, there was nothing
    /// to copy, or the copy failed and why.
    ///
    /// Never throws: a failed backup must not stop the app opening. But it must not be silent
    /// either, because the caller is about to rewrite the file it could not copy; the error travels
    /// in the result so the app can say so.
    /// </summary>
    public static BackupResult Create(ResumeDbContext context)
    {
        try
        {
            var path = PathOf(context);
            if (path is null || !File.Exists(path))
                return BackupResult.Skipped;

            // The whole reason this class exists. SQLite in WAL mode keeps recent writes in a
            // separate -wal file, so copying only the .db silently captures a database missing
            // everything since the last checkpoint - which can be most of it. Forcing a
            // checkpoint first is what makes the copy a real backup rather than a stale one.
            context.Database.ExecuteSqlRaw("PRAGMA wal_checkpoint(TRUNCATE);");

            var directory = Path.GetDirectoryName(path)!;
            var stamp = DateTime.UtcNow.ToString("yyyyMMdd-HHmmss");
            var destination = Path.Combine(directory, $"resumes.backup-{stamp}.db");

            File.Copy(path, destination, overwrite: true);
            Prune(directory);
            return BackupResult.Made(destination);
        }
        catch (Exception ex)
        {
            return BackupResult.Failed(ex.Message);
        }
    }

    /// <summary>Existing backups, newest first.</summary>
    public static IReadOnlyList<string> List(string directory)
    {
        if (!Directory.Exists(directory))
            return Array.Empty<string>();

        return Directory.GetFiles(directory, "resumes.backup-*.db")
            .OrderByDescending(f => f, StringComparer.Ordinal)
            .ToList();
    }

    private static void Prune(string directory)
    {
        foreach (var old in List(directory).Skip(KeepCount))
        {
            try
            {
                File.Delete(old);
            }
            catch (IOException)
            {
                // A backup that cannot be deleted is not worth failing over.
            }
        }
    }

    /// <summary>
    /// The file behind the connection. Returns null for in-memory databases, which the tests use
    /// and which have nothing to copy.
    /// </summary>
    public static string? PathOf(ResumeDbContext context)
    {
        var source = context.Database.GetDbConnection().DataSource;
        return string.IsNullOrWhiteSpace(source) || source.Contains(":memory:", StringComparison.OrdinalIgnoreCase)
            ? null
            : source;
    }
}

/// <summary>
/// What <see cref="DatabaseBackup.Create"/> did. Exactly one of the three is true: a copy exists at
/// <see cref="Path"/>, there was nothing to copy, or the copy failed for <see cref="Error"/>.
/// </summary>
public sealed record BackupResult(string? Path, string? Error)
{
    public static readonly BackupResult Skipped = new(null, null);
    public static BackupResult Made(string path) => new(path, null);
    public static BackupResult Failed(string error) => new(null, error);

    public bool Succeeded => Path is not null;
    public bool WasSkipped => Path is null && Error is null;
}
